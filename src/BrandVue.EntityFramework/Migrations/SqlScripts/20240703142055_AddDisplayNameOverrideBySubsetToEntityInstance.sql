UPDATE e
SET DisplayNameOverrideBySubset = (
	'{' + 
	STUFF(
		(SELECT ',  "' + Identifier + '": "' + e.DisplayName + '"' 
			FROM SubsetConfigurations 
			WHERE e.ProductShortCode = ProductShortCode and (e.SubProductId is null or e.SubProductId = SubProductId)
			FOR XML PATH ('')), 1, 1, ''
		 )  + '}'
)
FROM EntityInstanceConfigurations e
 