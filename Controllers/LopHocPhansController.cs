using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using UniSchedule.Models;

namespace UniSchedule.Controllers
{
    public class LopHocPhansController : Controller
    {
        private XepLichGiangVienEntities db = new XepLichGiangVienEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["MaVaiTro"] == null || (int)Session["MaVaiTro"] == 1)
            {
                filterContext.Result = RedirectToAction("Login", "Home");
            }
            base.OnActionExecuting(filterContext);
        }
        // GET: LopHocPhans
        public ActionResult Index()
        {
            var danhSachLHP = db.LopHocPhans.ToList();
            var danhSachPhanCong = db.PhanCongGiangDays.ToList();
            ViewBag.PhanCong = danhSachPhanCong;
            ViewBag.MaVaiTro = Session["MaVaiTro"];
            return View(danhSachLHP);
        }

        // GET: LopHocPhans/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            if (lopHocPhan == null)
            {
                return HttpNotFound();
            }
            return View(lopHocPhan);
        }

        // GET: LopHocPhans/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LopHocPhans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaLHP,TenMH,SoTinChi,SoTietMoiTuan,SoLuongSinhVien")] LopHocPhan lopHocPhan)
        {
            if ((lopHocPhan.MaLHP == null || lopHocPhan.MaLHP == "") || (lopHocPhan.TenMH == null || lopHocPhan.TenMH == ""))
            {
                ModelState.AddModelError("", "Thiếu thông tin");
                if (lopHocPhan.MaLHP == null || lopHocPhan.MaLHP == "")
                {
                    ModelState.AddModelError("MaLHP", "Mã lớp học phần không được để trống.");
                }
                if (lopHocPhan.TenMH == null || lopHocPhan.TenMH == "")
                {
                    ModelState.AddModelError("TenMH", "Hãy nhập tên môn học.");
                }
                return View(lopHocPhan);
            }
            if (ModelState.IsValid)
            {
                db.LopHocPhans.Add(lopHocPhan);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(lopHocPhan);
        }

        // GET: LopHocPhans/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            if (lopHocPhan == null)
            {
                return HttpNotFound();
            }
            return View(lopHocPhan);
        }

        // POST: LopHocPhans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaLHP,TenMH,SoTinChi,SoTietMoiTuan,SoLuongSinhVien")] LopHocPhan lopHocPhan)
        {
            if (lopHocPhan.TenMH == null || lopHocPhan.TenMH == "")
            {
                ModelState.AddModelError("", "Thiếu thông tin. ");
                ModelState.AddModelError("TenMH", "Hãy nhập tên môn học.");
                return View(lopHocPhan);
            }

            if (ModelState.IsValid)
            {
                db.Entry(lopHocPhan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(lopHocPhan);
        }

        // GET: LopHocPhans/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            if (lopHocPhan == null)
            {
                return HttpNotFound();
            }
            return View(lopHocPhan);
        }

        // POST: LopHocPhans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            LopHocPhan lopHocPhan = db.LopHocPhans.Find(id);
            bool isHavePhanCong = db.PhanCongGiangDays.Any(p => p.MaLHP == id);
            if (isHavePhanCong)
            {
                ModelState.AddModelError("", "Lớp học phần này đã được phân công.");
                return View(lopHocPhan);
            }
            db.LopHocPhans.Remove(lopHocPhan);
            db.SaveChanges();
            ModelState.AddModelError("", "Xóa thành công");
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}