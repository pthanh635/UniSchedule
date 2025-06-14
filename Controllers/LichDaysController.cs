﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using UniSchedule.Models;

namespace UniSchedule.Controllers
{
    public class LichDaysController : Controller
    {
        private XepLichGiangVienEntities db = new XepLichGiangVienEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["MaVaiTro"] == null)
            {
                filterContext.Result = RedirectToAction("Login", "Home");
            }
            base.OnActionExecuting(filterContext);
        }
        // GET: LichDays
        public ActionResult Index(int? hocKi, string namHoc, string maKhoa)
        {
            int maVaiTro = Session["MaVaiTro"] != null ? (int)Session["MaVaiTro"] : -1;

            ViewBag.MaVaiTro = maVaiTro;
            var lichList = db.LichDays
                .Include(l => l.PhongHoc)
                .Include(l => l.PhanCongGiangDay.GiangVien.Khoa)
                .Include(l => l.PhanCongGiangDay.LopHocPhan);

            if (hocKi.HasValue)
            {
                lichList = lichList.Where(l => l.PhanCongGiangDay.HocKi == hocKi);
            }

            if (!string.IsNullOrEmpty(namHoc))
            {
                lichList = lichList.Where(l => l.PhanCongGiangDay.NamHoc == namHoc);
            }

            if (!string.IsNullOrEmpty(maKhoa))
            {
                lichList = lichList.Where(l => l.PhanCongGiangDay.GiangVien.MaKhoa == maKhoa);
            }

            var hocKiList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Học kỳ 1" },
                new SelectListItem { Value = "2", Text = "Học kỳ 2" },
                new SelectListItem { Value = "3", Text = "Học kỳ hè" }
            };
            ViewBag.HocKiList = hocKiList;

            int currentYear = DateTime.Now.Year;
            var namHocList = new List<SelectListItem>
            {
                new SelectListItem { Value = $"{currentYear - 1}-{currentYear}", Text = $"{currentYear - 1}-{currentYear}" },
                new SelectListItem { Value = $"{currentYear}-{currentYear + 1}", Text = $"{currentYear}-{currentYear + 1}" }
            };
            ViewBag.NamHocList = namHocList;

            var khoaList = db.Khoas
                .Select(k => new SelectListItem
                {
                    Value = k.MaKhoa,
                    Text = k.TenKhoa
                }).ToList();
            ViewBag.KhoaList = khoaList;

            return View(lichList.ToList());
        }

        // GET: LichDays/XepLich
        public ActionResult XepLich()
        {
            var hocKiList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Học kỳ 1" },
                new SelectListItem { Value = "2", Text = "Học kỳ 2" },
                new SelectListItem { Value = "3", Text = "Học kỳ hè" }
            };
            ViewBag.HocKiList = hocKiList;

            int currentYear = DateTime.Now.Year;
            var namHocList = new List<SelectListItem>
            {
                new SelectListItem { Value = $"{currentYear - 1}-{currentYear}", Text = $"{currentYear - 1}-{currentYear}" },
                new SelectListItem { Value = $"{currentYear}-{currentYear + 1}", Text = $"{currentYear}-{currentYear + 1}" }
            };
            ViewBag.NamHocList = namHocList;

            return View();
        }

        // POST: LichDays/XepLich
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XepLich(int? hocKi, string namHoc)
        {
            if (!hocKi.HasValue || !new[] { 1, 2, 3 }.Contains(hocKi.Value))
            {
                TempData["Error"] = "Vui lòng chọn học kỳ.";
                return RedirectToAction("XepLich");
            }

            int currentYear = DateTime.Now.Year;
            var validNamHocs = new[] { $"{currentYear - 1}-{currentYear}", $"{currentYear}-{currentYear + 1}" };
            if (string.IsNullOrEmpty(namHoc) || !validNamHocs.Contains(namHoc))
            {
                TempData["Error"] = "Vui lòng chọn năm học.";
                return RedirectToAction("XepLich");
            }

            var phanCongs = db.PhanCongGiangDays
                .Where(p => p.HocKi == hocKi && p.NamHoc == namHoc)
                .ToList();

            var lichTam = new List<LichDay>();

            foreach (var pc in phanCongs)
            {
                if (db.LichDays.Any(ld => ld.MaPhanCong == pc.MaPhanCong))
                    continue;

                var phongPhuHop = db.PhongHocs
                    .Where(p => p.SoLuongChoNgoi >= pc.LopHocPhan.SoLuongSinhVien)
                    .OrderBy(r => Guid.NewGuid())
                    .ToList();

                foreach (var phong in phongPhuHop)
                {
                    var lich = GenerateLichDay(pc, phong.MaPhong, lichTam);
                    if (lich != null)
                    {
                        lichTam.Add(lich);
                        break;
                    }
                }
            }

            db.LichDays.AddRange(lichTam);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // Hàm sinh lịch
        private LichDay GenerateLichDay(PhanCongGiangDay pc, string maPhong, List<LichDay> lichTam)
        {
    
            string maGV = pc.MaGV;
            int soTiet = pc.LopHocPhan.SoTietMoiTuan;

            int tuanBatDau, tuanKetThuc;
            switch (pc.HocKi)
            {
                case 1:
                    tuanBatDau = 3; tuanKetThuc = 18; break;
                case 2:
                    tuanBatDau = 24; tuanKetThuc = 42; break;
                case 3:
                    tuanBatDau = 44; tuanKetThuc = 50; break;
                default:
                    return null;
            }

            LichDay bestSlot = null;
            int bestScore = int.MinValue;

            for (int thu = 2; thu <= 7; thu++)
            {
                for (int tietBD = 1; tietBD <= 10 - soTiet + 1; tietBD++)
                {
                    int tietKT = tietBD + soTiet - 1;

                    bool trungGV = lichTam.Any(ld =>
                    {
                        var pcLD = db.PhanCongGiangDays.Find(ld.MaPhanCong);
                        return pcLD.MaGV == maGV &&
                               ld.Thu == thu &&
                               !(ld.TietKetThuc < tietBD || ld.TietBatDau > tietKT) &&
                               !(ld.TuanKetThuc < tuanBatDau || ld.TuanBatDau > tuanKetThuc);
                    });

                    bool trungPhong = lichTam.Any(ld =>
                        ld.MaPhong == maPhong &&
                        ld.Thu == thu &&
                        !(ld.TietKetThuc < tietBD || ld.TietBatDau > tietKT) &&
                        !(ld.TuanKetThuc < tuanBatDau || ld.TuanBatDau > tuanKetThuc));

                    if (!trungGV && !trungPhong)
                    {
                        int score = CalculateScore(thu, tietBD, maGV, tietKT, lichTam);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSlot = new LichDay
                            {
                                MaPhanCong = pc.MaPhanCong,
                                MaPhong = maPhong,
                                Thu = thu,
                                TietBatDau = tietBD,
                                TietKetThuc = tietKT,
                                TuanBatDau = tuanBatDau,
                                TuanKetThuc = tuanKetThuc
                            };
                        }
                    }
                }
            }

            return bestSlot;
        }

        // Hàm tính điểm ưu tiên cho lịch
        private int CalculateScore(int thu, int tietBD, string maGV, int tietKT, List<LichDay> lichTam)
        {
            int score = 0;

            if (tietBD <= 5) score += 3;
            if (tietBD == 1 || tietBD == 4) score += 1;

            bool coTietLien = lichTam.Any(ld =>
            {
                var pcLD = db.PhanCongGiangDays.Find(ld.MaPhanCong);
                return pcLD.MaGV == maGV &&
                       ld.Thu == thu &&
                       (ld.TietKetThuc + 1 == tietBD || ld.TietBatDau - 1 == tietKT);
            });
            if (coTietLien) score += 4;

            int soBuoiTrongThuGV = lichTam.Count(ld =>
            {
                var pcLD = db.PhanCongGiangDays.Find(ld.MaPhanCong);
                return pcLD.MaGV == maGV && ld.Thu == thu;
            });
            score += (5 - soBuoiTrongThuGV);

            int tongLopTrongThu = lichTam.Count(ld => ld.Thu == thu);
            score += (10 - tongLopTrongThu);

            return score;
        }

        public ActionResult XemLich()
        {
            string maGV = Session["MaGV"].ToString();

            var lichList = db.LichDays
                .Include(l => l.PhongHoc)
                .Include(l => l.PhanCongGiangDay.LopHocPhan)
                .Where(l => l.PhanCongGiangDay.MaGV == maGV)
                .ToList();

            ViewBag.TenGV = db.GiangViens.FirstOrDefault(g => g.MaGV == maGV)?.TenGV;

            return View(lichList);
        }


        // GET: LichDays/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            LichDay lichDay = db.LichDays.Find(id);
            if (lichDay == null) return HttpNotFound();
            return View(lichDay);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            LichDay lichDay = db.LichDays.Find(id);
            if (lichDay == null) return HttpNotFound();
            ViewBag.MaPhong = new SelectList(db.PhongHocs, "MaPhong", "TenPhong", lichDay.MaPhong);
            ViewBag.MaPhanCong = new SelectList(db.PhanCongGiangDays, "MaPhanCong", "MaGV", lichDay.MaPhanCong);
            return View(lichDay);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaLich,Thu,TietBatDau,TietKetThuc,TuanBatDau,TuanKetThuc,MaPhong,MaPhanCong")] LichDay lichDay)
        {
            if (ModelState.IsValid)
            {
                db.Entry(lichDay).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaPhong = new SelectList(db.PhongHocs, "MaPhong", "TenPhong", lichDay.MaPhong);
            ViewBag.MaPhanCong = new SelectList(db.PhanCongGiangDays, "MaPhanCong", "MaGV", lichDay.MaPhanCong);
            return View(lichDay);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            LichDay lichDay = db.LichDays.Find(id);
            if (lichDay == null) return HttpNotFound();
            return View(lichDay);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            LichDay lichDay = db.LichDays.Find(id);
            db.LichDays.Remove(lichDay);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAll()
        {
            var allRecords = db.LichDays.ToList();
            db.LichDays.RemoveRange(allRecords);
            db.SaveChanges();

            TempData["Message"] = "Đã xóa toàn bộ lịch giảng dạy.";
            return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportToExcel(int? hocKi, string namHoc, string maKhoa)
        {
            var lichList = db.LichDays
                .Include(l => l.PhanCongGiangDay.GiangVien.Khoa)
                .Include(l => l.PhanCongGiangDay.LopHocPhan)
                .Include(l => l.PhongHoc)
                .AsQueryable();

            if (hocKi.HasValue)
                lichList = lichList.Where(l => l.PhanCongGiangDay.HocKi == hocKi);

            if (!string.IsNullOrEmpty(namHoc))
                lichList = lichList.Where(l => l.PhanCongGiangDay.NamHoc == namHoc);

            if (!string.IsNullOrEmpty(maKhoa))
                lichList = lichList.Where(l => l.PhanCongGiangDay.GiangVien.MaKhoa == maKhoa);

            var data = lichList.ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("LichDay");
                // Set column headers in the specified order
                worksheet.Cell(1, 1).Value = "Số thứ tự";
                worksheet.Cell(1, 2).Value = "Giảng Viên";
                worksheet.Cell(1, 3).Value = "Lớp Học Phần";
                worksheet.Cell(1, 4).Value = "Khoa";
                worksheet.Cell(1, 5).Value = "Phòng Học";
                worksheet.Cell(1, 6).Value = "Thứ";
                worksheet.Cell(1, 7).Value = "Tiết";
                worksheet.Cell(1, 8).Value = "Tuần";
                worksheet.Cell(1, 9).Value = "Học Kì";
                worksheet.Cell(1, 10).Value = "Năm Học";

                int row = 2;
                int stt = 1;

                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = stt++;
                    worksheet.Cell(row, 2).Value = item.PhanCongGiangDay?.GiangVien?.TenGV;
                    worksheet.Cell(row, 3).Value = item.PhanCongGiangDay?.LopHocPhan?.TenMH;
                    worksheet.Cell(row, 4).Value = item.PhanCongGiangDay?.GiangVien?.Khoa?.TenKhoa;
                    worksheet.Cell(row, 5).Value = item.PhongHoc?.TenPhong;
                    worksheet.Cell(row, 6).Value = item.Thu;
                    worksheet.Cell(row, 7).Value = $"{item.TietBatDau}-{item.TietKetThuc}";
                    worksheet.Cell(row, 8).Value = $"{item.TuanBatDau}-{item.TuanKetThuc}";
                    worksheet.Cell(row, 9).Value = item.PhanCongGiangDay?.HocKi;
                    worksheet.Cell(row, 10).Value = item.PhanCongGiangDay?.NamHoc;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = string.IsNullOrEmpty(maKhoa)
                        ? "LichDayDUT.xlsx"
                        : $"LichDayKhoa{maKhoa}.xlsx";

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
    }
}
