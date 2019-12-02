-- queries with "CREATED" status
Select count(*) from dbo.ActivityQueriesDB WHERE [Status]=0
-- athlete count
Select count(*) from dbo.AthleteShortDB
-- athlete count in activities
Select count(Distinct AthleteId) from dbo.ActivityShortDB 