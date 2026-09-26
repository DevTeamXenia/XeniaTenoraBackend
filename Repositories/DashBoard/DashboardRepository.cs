using Microsoft.EntityFrameworkCore;
using Stripe.Climate;
using XeniaRentalBackend.Controllers;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaRentalBackend.Service.Common;


namespace XeniaRentalBackend.Repositories.Dashboard
{
    public class DashboardRepository : IDashboardRepsitory
    {

        private readonly ApplicationDbContext _context;
        private readonly JwtHelperService _jwtHelperService;

        public DashboardRepository(ApplicationDbContext context, JwtHelperService jwtHelperService)
        {
            _context = context;
            _jwtHelperService = jwtHelperService;
        }

        public async Task<RentDashboardDto> GetRentDashboardAsync(int companyId, int userId , DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var to = toDate.Date;

            List<int>? userPropertyIds = null;

            userPropertyIds = await _context.UserMapping
                .Where(m => m.UserID == userId && m.IsActive)
                .Select(m => m.PropID)
                .ToListAsync();
            

            var activeAssignments = await _context.TenantAssignemnts
                .Where(t =>
                    t.companyID == companyId &&
                    !t.isClosure &&
                    t.agreementStartDate.Date <= to &&
                    t.agreementEndDate.Date >= from &&
                    (userPropertyIds == null || userPropertyIds.Contains(t.propID))  
                )
                .ToListAsync();

            var vouchers = await _context.Vouchers
                .Where(v => v.CompanyID == companyId &&
                            v.VoucherType == "Pay Rent" &&
                            v.VoucherDate >= fromDate &&
                            v.VoucherDate <= toDate &&
                            (userPropertyIds == null || userPropertyIds.Contains(v.PropID))) 
                .ToListAsync();

            int paidCount = 0;
            decimal totalPaidAmount = 0;
            int notPaidCount = 0;
            decimal totalNotPaidAmount = 0;

            foreach (var tenant in activeAssignments)
            {
                var voucher = vouchers.FirstOrDefault(v => v.DrID == tenant.tenantID && v.unitID == tenant.unitID);
                if (voucher != null)
                {
                    paidCount++;
                    totalPaidAmount += voucher.Amount;
                }
                else
                {
                    notPaidCount++;
                    totalNotPaidAmount += tenant.rentAmt;
                }
            }

            var occupiedPropertyIds = activeAssignments.Select(t => t.propID).Distinct().Count();
            var occupiedUnitIds = activeAssignments.Select(t => t.unitID).Distinct().Count();

            var totalPropertiesCount = await _context.Properties
                .Where(p => p.CompanyId == companyId &&
                            (userPropertyIds == null || userPropertyIds.Contains(p.PropID)))  
                .CountAsync();

            var totalUnitsCount = await _context.Units
                .Where(u => u.CompanyId == companyId &&
                            (userPropertyIds == null || userPropertyIds.Contains(u.PropID))) 
                .CountAsync();

            int vacantProperties = totalPropertiesCount - occupiedPropertyIds;
            int vacantUnits = totalUnitsCount - occupiedUnitIds;

            decimal averageRentPerProperty = occupiedPropertyIds > 0
                ? totalPaidAmount / occupiedPropertyIds
                : 0;

            int highRiskTenantCount = activeAssignments.Count(t =>
                !vouchers.Any(v => v.DrID == t.tenantID && v.unitID == t.unitID));

            int highRiskPropertyCount = activeAssignments
                .Where(t => !vouchers.Any(v => v.DrID == t.tenantID && v.unitID == t.unitID))
                .Select(t => t.propID)
                .Distinct()
                .Count();

            decimal occupancyRate = totalPropertiesCount > 0
                ? (decimal)occupiedPropertyIds / totalPropertiesCount * 100
                : 0;

            decimal collectionRate = (paidCount + notPaidCount) > 0
                ? (decimal)paidCount / (paidCount + notPaidCount) * 100
                : 0;

            var topPerformingProperties = vouchers
                .GroupBy(v => v.PropID)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    TotalCollected = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalCollected)
                .Take(3)
                .ToList();

            int topPerformingPropertyCount = topPerformingProperties.Count;

            return new RentDashboardDto
            {
                TotalPaidCount = paidCount,
                TotalPaidAmount = totalPaidAmount,
                TotalNotPaidCount = notPaidCount,
                TotalNotPaidAmount = totalNotPaidAmount,
                TotalOccupiedProperties = occupiedPropertyIds,
                TotalOccupiedUnits = occupiedUnitIds,
                VacantProperties = vacantProperties,
                VacantUnits = vacantUnits,
                TopPerformingPropertyCount = topPerformingPropertyCount,
                AverageRentPerProperty = averageRentPerProperty,
                HighRiskTenantCount = highRiskTenantCount,
                HighRiskPropertyCount = highRiskPropertyCount,
                OccupancyRate = Math.Round(occupancyRate, 2),
                CollectionRate = Math.Round(collectionRate, 2)
            };
        }
        public async Task<MonthlyRevenueResponseDto> GetMonthlyRentRevenueAsync(int companyid, int userId ,int year)
        {
            var company = await _context.Company
                .Where(c => c.companyID == companyid && c.IsActive)
                .Select(c => new
                {
                    c.companyID,
                    c.companyName,
                    c.logo
                })
                .FirstOrDefaultAsync();

            if (company == null)
                throw new Exception("Company not found");

            
            var mappedPropertyIds = await _context.UserMapping
                .Where(x => x.UserID == userId && x.IsActive)
                .Select(x => x.PropID)
                .ToListAsync();

            var vouchers = await _context.Vouchers
                .Where(v =>
                    v.CompanyID == companyid &&
                    v.VoucherType == "Pay Rent" &&
                    v.VoucherDate.Year == year &&
                    mappedPropertyIds.Contains(v.PropID))
                .ToListAsync();

            var monthlyRevenue = Enumerable.Range(1, 12)
                .Select(month => new MonthlyRevenueDto
                {
                    Month = new DateTime(year, month, 1).ToString("MMM"),
                    TotalRent = vouchers
                        .Where(v => v.VoucherDate.Month == month)
                        .Sum(v => v.Amount)
                })
                .ToList();

            return new MonthlyRevenueResponseDto
            {
                CompanyId = company.companyID,
                CompanyName = company.companyName,
                CompanyLogo = company.logo,
                MonthlyRevenue = monthlyRevenue
            };
        }

        public async Task<TenantPaymentSummaryDto> GetTenantPaymentsAsync(int unitId)
        {
            var today = DateTime.Today;

            int tenantId = _jwtHelperService.GetTenantIdId();

            var assignment = await _context.TenantAssignemnts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.tenantID == tenantId &&
                    x.unitID == unitId &&
                    x.isActive);

            if (assignment == null)
            {
                return new TenantPaymentSummaryDto
                {
                    PreviousUnpaidPayments = new List<UpcomingPaymentDto>(),
                    PreviousPaidPayments = new List<PaidPaymentDto>(),
                    NextUpcomingPayment = null
                };
            }

            int intervalMonths = assignment.collectionType?.ToLower() switch
            {
                "monthly" => 1,
                "2months" => 2,
                "quarterly" => 3,
                "6months" => 6,
                "yearly" => 12,
                _ => 1
            };

            var agreementStart = assignment.agreementStartDate.Date;

            var agreementEnd = assignment.agreementEndDate.Date;

            int dueDay = assignment.rentCollection <= 0
                ? 1
                : assignment.rentCollection;

            var firstMonth = new DateTime(
                agreementStart.Year,
                agreementStart.Month,
                1).AddMonths(intervalMonths);

            int firstValidDay = Math.Min(
                dueDay,
                DateTime.DaysInMonth(
                    firstMonth.Year,
                    firstMonth.Month));

            var firstDueDate = new DateTime(
                firstMonth.Year,
                firstMonth.Month,
                firstValidDay);

    
            DateTime chargeAnchorDate = firstMonth;

            var vouchers = await _context.Vouchers
                .AsNoTracking()
                .Where(v =>
                    v.CrID == tenantId &&
                    v.unitID == unitId &&
                    !v.Cancelled)
                .Select(v => new
                {
                    v.VoucherID,
                    v.VoucherDate,
                    v.VoucherStatus,
                    v.Amount,
                    v.RentMonth,
                    v.RentYear
                })
                .ToListAsync();

            var paidVoucherLookup = vouchers
                .Where(v =>
                    v.VoucherStatus != null &&
                    v.VoucherStatus.ToUpper() == "PAID")
                .GroupBy(v => new
                {
                    v.RentYear,
                    v.RentMonth
                })
                .ToDictionary(
                    g => $"{g.Key.RentYear}_{g.Key.RentMonth}",
                    g => g.First());

            var voucherIds = vouchers
                .Select(v => v.VoucherID)
                .Distinct()
                .ToList();

            var voucherDetails = await (
                from detail in _context.VoucherDetails.AsNoTracking()

                join charge in _context.Charges.AsNoTracking()
                    on detail.chargeId equals charge.chargeID

                where voucherIds.Contains(detail.voucherId)

                select new
                {
                    detail.voucherId,
                    detail.amount,

                    charge.chargeID,
                    charge.chargeName,
                    charge.isVariable
                }
            ).ToListAsync();

            var voucherChargeLookup = voucherDetails
                .GroupBy(x => x.voucherId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());

       
            var voucherInfoLookup = vouchers
                .ToDictionary(v => v.VoucherID, v => new { v.RentYear, v.RentMonth });

            var voucherChargeHistory = (
                from vd in voucherDetails
                where voucherInfoLookup.ContainsKey(vd.voucherId)
                let info = voucherInfoLookup[vd.voucherId]
                select new
                {
                    vd.chargeID,
                    info.RentYear,
                    info.RentMonth,
                    vd.amount
                }
            ).ToList();

            var lastChargedLookup = voucherChargeHistory
                .GroupBy(x => x.chargeID)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g.OrderByDescending(x => x.RentYear)
                                      .ThenByDescending(x => x.RentMonth)
                                      .First();
                        return (RentYear: latest.RentYear, RentMonth: latest.RentMonth, Amount: latest.amount);
                    }
                );

            var unitCharges = await (
                from mapping in _context.UnitChargesMappings.AsNoTracking()

                join charge in _context.Charges.AsNoTracking()
                    on mapping.chargeID equals charge.chargeID

                where mapping.unitID == unitId
                      && mapping.isActive
                      && charge.isActive

                select new
                {
                    ChargeId = mapping.chargeID,
                    ChargeName = charge.chargeName,
                    ChargeAmount = mapping.amount,
                    Frequency = mapping.frequency,
                    IsVariable = charge.isVariable
                }
            ).ToListAsync();

            int GetFrequencyMonths(string? frequency) => frequency?.Trim().ToLower() switch
            {
                "monthly" => 1,
                "2months" => 2,
                "quarterly" => 3,
                "6months" => 6,
                "yearly" => 12,
                _ => 1
            };

            // FIX: replaced the simple "months-since-agreementStart % interval == 0"
            // check with the same last-actual-charge-anchored logic used elsewhere.
            // The old version always measured from agreementStart, so once a charge
            // had actually been billed mid-cycle (off the original anchor), it would
            // keep firing/skipping on the wrong months going forward. This version
            // anchors to the last real invoice date for that charge once one exists,
            // and falls back to the agreement-start anchor only before the first
            // invoice ever happens.
            bool IsChargeDue(int chargeId, string? frequency, DateTime dueDate)
            {
                int intervalMonthsForCharge = GetFrequencyMonths(frequency);

                if (!lastChargedLookup.TryGetValue(chargeId, out var last))
                {
                    int monthsSinceStart =
                        ((dueDate.Year - chargeAnchorDate.Year) * 12) +
                        (dueDate.Month - chargeAnchorDate.Month);

                    if (monthsSinceStart < 0)
                    {
                        return false;
                    }

                    return monthsSinceStart % intervalMonthsForCharge == 0;
                }

                int monthsSinceLast =
                    ((dueDate.Year - last.RentYear) * 12) + (dueDate.Month - last.RentMonth);

                return monthsSinceLast > 0 && monthsSinceLast % intervalMonthsForCharge == 0;
            }

            var rentSchedule = new List<UpcomingPaymentDto>();

            var currentDueDate = firstDueDate; while (currentDueDate <= agreementEnd)
            {
                string key = $"{currentDueDate.Year}_{currentDueDate.Month}";

                paidVoucherLookup.TryGetValue(key, out var paidVoucher);

                if (paidVoucher == null)
                {
                    decimal totalAmount = assignment.rentAmt;

                    string remarks = "Pending";

                    List<ChargeDetailDto> charges = new();

                    var monthVoucher = vouchers.FirstOrDefault(v =>
                        v.RentYear == currentDueDate.Year &&
                        v.RentMonth == currentDueDate.Month);

                    if (monthVoucher != null)
                    {
                        remarks = string.IsNullOrWhiteSpace(monthVoucher.VoucherStatus)
                            ? "Initiated"
                            : monthVoucher.VoucherStatus;

                        if (voucherChargeLookup.TryGetValue(monthVoucher.VoucherID, out var voucherCharges))
                        {
                            charges = voucherCharges
                                .Select(x => new ChargeDetailDto
                                {
                                    ChargeId = x.chargeID,
                                    ChargeName = x.chargeName,
                                    ChargeAmount = x.amount,
                                    IsVariable = x.isVariable,
                                    Status = remarks
                                })
                                .ToList();

                            totalAmount += charges.Sum(x => x.ChargeAmount);
                        }
                    }
                    else
                    {
                        charges = unitCharges
                            .Where(x => IsChargeDue(x.ChargeId, x.Frequency, currentDueDate))
                            .Select(x => new ChargeDetailDto
                            {
                                ChargeId = x.ChargeId,
                                ChargeName = x.ChargeName,
                                // FIX: variable charges now carry forward the last actually
                                // invoiced amount (when one exists) instead of always using
                                // the UnitChargesMappings placeholder amount, which is often 0.
                                ChargeAmount = x.IsVariable && lastChargedLookup.TryGetValue(x.ChargeId, out var lastCharge)
                                    ? lastCharge.Amount
                                    : x.ChargeAmount,
                                IsVariable = x.IsVariable,
                                Status = "Not Initiated"
                            })
                            .ToList();

                        totalAmount += charges.Sum(x => x.ChargeAmount);

                        remarks = charges.Any()
                            ? "Not Initiated"
                            : "Pending";
                    }

                    rentSchedule.Add(new UpcomingPaymentDto
                    {
                        RentDueDate = currentDueDate.ToString("dd MMM yyyy"),
                        RentAmount = totalAmount,
                        Remarks = remarks,
                        Charges = charges
                    });
                }

                var nextMonth = currentDueDate.AddMonths(intervalMonths);

                int validDay = Math.Min(
                    dueDay,
                    DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));

                currentDueDate = new DateTime(
                    nextMonth.Year,
                    nextMonth.Month,
                    validDay);
            }
            var previousUnpaidPayments = rentSchedule
        .Where(x =>
            DateTime.ParseExact(
                x.RentDueDate,
                "dd MMM yyyy",
                null) < today)
        .OrderByDescending(x =>
            DateTime.ParseExact(
                x.RentDueDate,
                "dd MMM yyyy",
                null))
        .ToList();

            var nextUpcomingPayment = rentSchedule
                .Where(x =>
                    DateTime.ParseExact(
                        x.RentDueDate,
                        "dd MMM yyyy",
                        null) >= today)
                .OrderBy(x =>
                    DateTime.ParseExact(
                        x.RentDueDate,
                        "dd MMM yyyy",
                        null))
                .FirstOrDefault();

            var previousPaidPayments = await _context.Vouchers
                .AsNoTracking()
                .Where(v =>
                    v.CrID == tenantId &&
                    v.unitID == unitId &&
                    v.VoucherStatus == "PAID" &&
                    !v.Cancelled)
                .OrderByDescending(v => v.VoucherDate)
                .Take(2)
                .Select(v => new PaidPaymentDto
                {
                    VoucherId = v.VoucherID,
                    VoucherDate = v.VoucherDate,
                    Amount = v.Amount
                })
                .ToListAsync();

            return new TenantPaymentSummaryDto
            {
                PreviousUnpaidPayments = previousUnpaidPayments,
                NextUpcomingPayment = nextUpcomingPayment,
                PreviousPaidPayments = previousPaidPayments
            };
        }
    }
}
