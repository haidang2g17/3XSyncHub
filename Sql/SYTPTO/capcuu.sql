SELECT json_build_object(
    'NGAY_GUI', TO_CHAR(NOW(), 'YYYYMMDDHH24MI'),
    'MACSKCB', '25001',
    'DANHSACH', json_agg(
        json_build_object(
            'MA_LK', m.vienphiid,
            'MA_BN', h.patientcode,
            'HO_TEN', h.patientname,
            'MA_THE', b.bhytcode,
            'NGAY_SINH', TO_CHAR(h.birthday, 'YYYYMMDD'),
            'DIA_CHI', CONCAT_WS(' - ', NULLIF(h.hc_thon, ''), NULLIF(h.hc_xaname, ''), NULLIF(h.hc_huyenname, ''), NULLIF(h.hc_tinhname, '')),
            'NGAY_VAO', TO_CHAR(v.vienphidate, 'YYYYMMDDHH24MI'),
            'NGAY_RA', CASE WHEN v.vienphidate_ravien = TO_DATE('0001-01-01 00:00:00', 'YYYY-MM-DD HH24:MI:SS') THEN '' ELSE TO_CHAR(v.vienphidate_ravien, 'YYYYMMDDHH24MI') END,
            'MA_KHOA', d.departmentgroupcode_byt,
            'TEN_KHOA', d.departmentgroupname,
            'MA_BENH', m.chandoanravien_code,
            'TEN_BENH', m.chandoanravien,
            'MA_BENHKHAC', '',
            'TEN_BENHKHAC', ''
        )
    )
) AS capcuu
FROM medicalrecord m
JOIN hosobenhan h ON m.hosobenhanid = h.hosobenhanid
JOIN vienphi v ON v.vienphiid = m.vienphiid
LEFT JOIN departmentgroup d ON d.departmentgroupid = v.departmentgroupid
LEFT JOIN department d2 ON d2.departmentid = v.departmentid
LEFT JOIN bhyt b ON b.bhytid = m.bhytid
WHERE m.hinhthucvaovienid = 1 
  AND m.thoigianvaovien >= date_trunc('day', now());