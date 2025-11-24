SELECT json_build_object(
    'NGAY_GUI', TO_CHAR(NOW(), 'YYYYMMDDHH24MI'),
    'MACSKCB', '25001',
    'DANHSACH', json_agg(
        json_build_object(
            'MA_LK', m_last.vienphiid::text,
            'MA_BN', h.patientcode,
            'HO_TEN', h.patientname,
            'MA_THE', b.bhytcode,
            'NGAY_SINH', TO_CHAR(h.birthday, 'YYYYMMDD') || '0000',
            'DIA_CHI', CONCAT_WS(' - ', NULLIF(h.hc_thon,''), NULLIF(h.hc_xaname,''), NULLIF(h.hc_huyenname,''), NULLIF(h.hc_tinhname,'')),
            'SO_DIENTHOAI', p.patientphone,
            'NGAY_VAO', TO_CHAR(m_last.thoigianvaovien, 'YYYYMMDDHH24MI'),
            'NGAY_RA', CASE 
                          WHEN h.hosobenhandate_ravien = TO_DATE('0001-01-01 00:00:00','YYYY-MM-DD HH24:MI:SS') 
                          THEN '' 
                          ELSE TO_CHAR(h.hosobenhandate_ravien, 'YYYYMMDDHH24MI') 
                       END,
            'NGAY_KHOIPHAT','',
            'MA_KHOA', d.departmentgroupcode_byt,
            'TEN_KHOA', d.departmentgroupname,
            'MA_BENH', m_last.chandoanravien_code,
            'TEN_BENH', m_last.chandoanravien,
            'MA_BENHKHAC', m_last.chandoanravien_kemtheo_code,
            'TEN_BENHKHAC', m_last.chandoanravien_kemtheo,
            'TINHTRANG_VAOVIEN', m_last.quatrinhbenhly,
            'TINHTRANG_HIENTAI', NULL,
            'CAPDO_CHAMSOC', NULL,
            'TRANG_THAI', 0,
            'THUOC_DA_KE', NULL
        )
    )
)
FROM hosobenhan h
JOIN (
    SELECT DISTINCT ON (hosobenhanid)
        hosobenhanid,
        vienphiid,
        bhytid,
        departmentgroupid,
        chandoanravien_code,
        chandoanravien,
        chandoanravien_kemtheo_code,
        chandoanravien_kemtheo,
        quatrinhbenhly,
        thoigianvaovien
    FROM medicalrecord
    WHERE chandoanravien_code IN ('A80','A36','B95','J10','A20','A98.4','A96.2','A98.3','B06', 
                                  'A92.3','A95','A97','A97.0','A97.1','A97.2','B05','A00',
                                  'B08.4','A22','A39.0','U07.1','H10.2')
    ORDER BY hosobenhanid, thoigianvaovien DESC
) m_last ON m_last.hosobenhanid = h.hosobenhanid
LEFT JOIN bhyt b ON b.bhytid = m_last.bhytid
LEFT JOIN departmentgroup d ON d.departmentgroupid = m_last.departmentgroupid
LEFT JOIN patient p ON p.patientid = h.patientid
WHERE 
    h.hosobenhandate_ravien = TO_DATE('0001-01-01 00:00:00','YYYY-MM-DD HH24:MI:SS')
    AND h.hosobenhandate >= current_date - INTERVAL '60 days';