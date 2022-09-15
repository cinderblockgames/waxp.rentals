CREATE PROCEDURE [reporting].[MonthlyStats]
AS
BEGIN

	;WITH Months AS (
		SELECT DISTINCT Year = DATEPART(year, Paid), Month = DATEPART(month, Paid) FROM Rental WHERE StatusId > 1
		UNION
		SELECT DISTINCT Year = DATEPART(year, Inserted), Month = DATEPART(month, Inserted) FROM Purchase WHERE StatusId > 1
		UNION
		SELECT DISTINCT Year = DATEPART(year, Paid), Month = DATEPART(month, Paid) FROM welcome.Package WHERE StatusId > 1
		UNION
		SELECT DISTINCT Year = DATEPART(year, Paid), Month = DATEPART(month, Paid) FROM welcome.PackageWithKeys WHERE StatusId > 1
		UNION
		SELECT Year = DATEPART(year, GETUTCDATE()), Month = DATEPART(month, GETUTCDATE()) /* Always include the current month. */
	)
	SELECT
		Months.Year,
		Months.Month,
		WaxDaysRented = ISNULL(WaxDaysRented, 0),
		WaxDaysFree = ISNULL(WaxDaysFree, 0),
		WaxPurchasedForSite = ISNULL(WaxPurchasedForSite, 0),
		WelcomePackagesOpened = ISNULL(WelcomePackagesOpened, 0) + ISNULL(WelcomePackagesWithKeysOpened, 0)
	FROM Months
	LEFT JOIN (
		SELECT Year, Month, WaxDaysRented = SUM(WaxDaysBought), WaxDaysFree = SUM(WaxDaysFree) FROM (
			SELECT
				Year = DATEPART(year, Paid),
				Month = DATEPART(month, Paid),
				WaxDaysBought = CASE WHEN Banano > 0 THEN RentalDays * (CPU + NET) ELSE 0 END,
				WaxDaysFree = CASE WHEN Banano = 0 THEN RentalDays * (CPU + NET) ELSE 0 END
			FROM Rental
			WHERE StatusId > 1
		) r
		GROUP BY Year, Month
	) r ON Months.Year = r.Year AND Months.Month = r.Month
	LEFT JOIN (
		SELECT Year, Month, WaxPurchasedForSite = SUM(WaxBought) FROM (
			SELECT
				Year = DATEPART(year, Inserted),
				Month = DATEPART(month, Inserted),
				WaxBought = CASE WHEN BananoTransaction IS NOT NULL THEN Wax ELSE 0 END
			FROM Purchase
			WHERE StatusId > 1
		) p
		GROUP BY Year, Month
	) p ON Months.Year = p.Year AND Months.Month = p.Month
	LEFT JOIN (
		SELECT Year, Month, WelcomePackagesOpened = COUNT(1) FROM (
			SELECT
				Year = DATEPART(year, Paid),
				Month = DATEPART(month, Paid)
			FROM welcome.Package
			WHERE StatusId > 1
		) wp
		GROUP BY Year, Month
	) wp ON Months.Year = wp.Year AND Months.Month = wp.Month
	LEFT JOIN (
		SELECT Year, Month, WelcomePackagesWithKeysOpened = COUNT(1) FROM (
			SELECT
				Year = DATEPART(year, Paid),
				Month = DATEPART(month, Paid)
			FROM welcome.PackageWithKeys
			WHERE StatusId > 1
		) wp
		GROUP BY Year, Month
	) wpk ON Months.Year = wpk.Year AND Months.Month = wpk.Month
	WHERE Months.Year IS NOT NULL AND Months.Month IS NOT NULL
	ORDER BY Months.Year DESC, Months.Month DESC;

END
