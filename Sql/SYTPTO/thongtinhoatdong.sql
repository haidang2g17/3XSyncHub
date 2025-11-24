SELECT json_build_object(
    'NGAY_GUI', TO_CHAR(NOW(), 'YYYYMMDDHH24MI'),
    'MACSKCB', '25001',
    'NOI_TRU', (
        SELECT COUNT(*)
        FROM hosobenhan h
        WHERE h.hinhthucvaovienid = 2 
          AND h.hosobenhanstatus = 0 
          AND h.hosobenhandate >= current_date - INTERVAL '60 days'
    ),
    'CHO_KHAM', (
        SELECT COUNT(*)
        FROM sothutuPhongKham s
        WHERE s.SoThuTuDate >= date_trunc('day', now())
          AND s.departmentid NOT IN (877,848,839,837,772,766,754,692,677,631,630,627,626,623,764)
          AND s.sothutustatus = 0
    ),
    'DA_KHAM', (
        SELECT COUNT(*)
        FROM sothutuPhongKham s
        WHERE s.SoThuTuDate >= date_trunc('day', now())
          AND s.departmentid NOT IN (877,848,839,837,772,766,754,692,677,631,630,627,626,623,764)
          AND s.sothutustatus IN (1,2)
    ),
    'RA_VIEN', (
        SELECT COUNT(*)
        FROM medicalrecord m
        WHERE m.thoigianravien >= date_trunc('day', now())
          AND m.dm_maloaikcbid = 3
          AND m.hinhthucravienid <> 8
    ),
    'CHUYEN_VIEN_NGOAI_TRU', (
        SELECT COUNT(*)
        FROM medicalrecord m join hosobenhan h on m.hosobenhanid = h.hosobenhanid
        WHERE h.hosobenhandate_ravien>= date_trunc('day', now())
          AND m.dm_maloaikcbid IN (1,2)
          AND m.xutrikhambenhid = 5
    ),
    'CHUYEN_VIEN_NOI_TRU', (
        SELECT COUNT(*)
        FROM medicalrecord m join hosobenhan h on m.hosobenhanid = h.hosobenhanid 
        WHERE h.hosobenhandate_ravien >= date_trunc('day', now())
          AND m.dm_maloaikcbid = 3
          AND m.hinhthucravienid = 5
    ),
    'CAP_CUU', (
        SELECT COUNT(DISTINCT m.hosobenhanid)
        FROM medicalrecord m
        WHERE m.hinhthucvaovienid = 1
          AND m.thoigianvaovien >= date_trunc('day', now())
    ),
    'PHAU_THUAT', (
        SELECT COUNT(DISTINCT s.hosobenhanid)
        FROM serviceprice s
        WHERE s.bhyt_groupcode = '06PTTT'
          AND s.servicepricedatesudung >= date_trunc('day', now())
    ),
    'BENH_TRUYENNHIEM_NGUYHIEM', (
        SELECT COUNT(DISTINCT h.hosobenhanid)
        FROM hosobenhan h
        JOIN medicalrecord m ON m.hosobenhanid = h.hosobenhanid
        WHERE h.hosobenhandate_ravien = TO_DATE('0001-01-01 00:00:00','YYYY-MM-DD HH24:MI:SS')
          AND m.chandoanravien_code IN ('A80','A36','B95','J10','A20','A98.4','A96.2','A98.3','B06','A92.3',
                                        'A95','A97','A97.0','A97.1','A97.2','B05','A00','B08.4','A22','A39.0',
                                        'U07.1','H10.2')
          AND h.hosobenhandate > current_date - INTERVAL '90 days'
    )
) AS thongtinhoatdong;