SELECT json_build_object(
    'NGAY_GUI', TO_CHAR(NOW(), 'YYYYMMDDHH24MI'),
    'MACSKCB', '25001',
    'DANHSACH', json_agg(
        json_build_object(
            'MA_LK', s.vienphiid::text,
            'MA_BN', h.patientcode,
            'HO_TEN', h.patientname,
            'MA_THE', b.bhytcode,
            'NGAY_SINH', TO_CHAR(h.birthday, 'YYYYMMDD') || '0000',
            'DIA_CHI', CONCAT_WS(' - ', NULLIF(h.hc_thon,''), NULLIF(h.hc_xaname,''), NULLIF(h.hc_huyenname,''), NULLIF(h.hc_tinhname,'')),
            'NGAY_VAO', TO_CHAR(h.hosobenhandate, 'YYYYMMDDHH24MI'),
            'NGAY_RA', CASE WHEN h.hosobenhandate_ravien = TO_DATE('0001-01-01 00:00:00','YYYY-MM-DD HH24:MI:SS') 
                            THEN '' ELSE TO_CHAR(h.hosobenhandate_ravien, 'YYYYMMDDHH24MI') END,
            'MA_KHOA', d.departmentgroupcode_byt,
            'TEN_KHOA', d.departmentgroupname,
            'MA_BENH', m2.chandoan_code,
            'TEN_BENH', m2.chandoan,
            'MA_DICHVU', s.servicepricecode,
            'TEN_DICHVU', s.servicepricename
        )
    )
)
FROM serviceprice s
JOIN hosobenhan h ON s.hosobenhanid = h.hosobenhanid
JOIN maubenhpham m2 ON m2.maubenhphamid = s.maubenhphamid
JOIN medicalrecord m ON m.medicalrecordid = s.medicalrecordid
LEFT JOIN departmentgroup d ON d.departmentgroupid = h.departmentgroupid
LEFT JOIN bhyt b ON b.bhytid = m.bhytid
WHERE s.bhyt_groupcode = '06PTTT'
  AND s.servicepricedatesudung >= date_trunc('day', now());