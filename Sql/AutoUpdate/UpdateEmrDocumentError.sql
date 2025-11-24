UPDATE emr_document 
SET errorID = 0  
WHERE errorID = 3 
  AND documentpath <> '' 
  AND createdate >= current_date - INTERVAL '5 days';
