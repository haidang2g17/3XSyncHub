SELECT json_build_object(
    'NGAY_GUI', TO_CHAR(NOW(), 'YYYYMMDDHH24MI'),
    'MACSKCB', '25001',
    'DANHSACH', COALESCE(
        json_agg(
            json_build_object(
                'MA_LK', m.vienphiid,
                'MA_BN', h.patientcode,
                'HO_TEN', h.patientname,
                'MA_THE', b.bhytcode,
                'NGAY_SINH', TO_CHAR(h.birthday, 'YYYYMMDD'),
                'DIA_CHI', CONCAT_WS(' - ',
                    NULLIF(h.hc_thon, ''),
                    NULLIF(h.hc_xaname, ''),
                    NULLIF(h.hc_huyenname, ''),
                    NULLIF(h.hc_tinhname, '')
                ),
                'NGAY_VAO', TO_CHAR(v.vienphidate, 'YYYYMMDDHH24MI'),
                'NGAY_RA', TO_CHAR(v.vienphidate_ravien, 'YYYYMMDDHH24MI'),
                'MA_KHOA', d.departmentgroupcode_byt,
                'TEN_KHOA', d.departmentgroupname,
                'MA_CSKCB_NOI_CHUYEN', c.benhvienchuyentoi_code,
                'TEN_CSKCB_NOI_CHUYEN', b2.benhvienname,
                'MA_BENH', h.chandoanravien_code,
                'TEN_BENH', h.chandoanravien,
                'MA_LOAI_KCB', m.dm_maloaikcbid
            )
        ),
        '[]'::json  -- 👈 đảm bảo nếu không có dữ liệu thì DANHSACH = []
    )
) AS CHUYEN_VIEN_JSON
FROM chuyenvien c
JOIN medicalrecord m ON c.medicalrecordid = m.medicalrecordid
JOIN hosobenhan h ON m.hosobenhanid = h.hosobenhanid
JOIN vienphi v ON v.vienphiid = m.vienphiid
LEFT JOIN bhyt b ON b.bhytid = m.bhytid
LEFT JOIN departmentgroup d ON d.departmentgroupid = v.departmentgroupid
LEFT JOIN department d2 ON d2.departmentid = v.departmentid
LEFT JOIN benhvien b2 ON c.benhvienchuyentoi_code = b2.benhviencode
WHERE h.hosobenhandate_ravien >= date_trunc('day', now());
