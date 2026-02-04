UPDATE EntitySetConfigurations SET
	Organisation = 'Savanta'
WHERE
	(Organisation = '' OR Organisation IS NULL)
	AND EntityType = 'Brand'
	AND IsSectorSet = 0
	AND Subset IS NOT NULL
	AND IsFallback = 0
