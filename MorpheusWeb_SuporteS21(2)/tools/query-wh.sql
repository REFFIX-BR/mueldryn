SELECT AccountID, Money, LEN(Items) AS items_len
FROM warehouse
WHERE AccountID = 'testgm';
