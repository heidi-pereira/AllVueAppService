UPDATE pg
SET pg.DefaultBase = 'L12M_base'
FROM Pages pg
JOIN Panes pn
ON (pn.PageName = pg.Name AND pn.ProductShortCode = pg.ProductShortCode)
WHERE PaneType = 'AudienceProfile'