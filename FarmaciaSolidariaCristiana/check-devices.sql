SELECT TOP 10 
    u.UserName, 
    u.Email,
    t.OneSignalPlayerId,
    t.DeviceType,
    t.IsActive,
    t.CreatedAt,
    t.UpdatedAt
FROM UserDeviceTokens t
JOIN AspNetUsers u ON t.UserId = u.Id
ORDER BY t.CreatedAt DESC
