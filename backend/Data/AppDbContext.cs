using Microsoft.EntityFrameworkCore;
using KtxAI.Api.Models;

namespace KtxAI.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<SinhVien> SinhViens { get; set; }
        public DbSet<ToaNha> ToaNhas { get; set; }
        public DbSet<Tang> Tangs { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<Giuong> Giuongs { get; set; }
        public DbSet<HopDong> HopDongs { get; set; }
        public DbSet<Phi> Phis { get; set; }
        public DbSet<PhanAnh> PhanAnhs { get; set; }
        public DbSet<ViPham> ViPhams { get; set; }
        public DbSet<NoiQuy> NoiQuys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Phi>().Property(x => x.SoTien).HasPrecision(18, 2);

            // Lưu enum dưới dạng chuỗi cho dễ đọc trực tiếp trong DB khi demo
            modelBuilder.Entity<NguoiDung>().Property(x => x.VaiTro).HasConversion<string>();
            modelBuilder.Entity<SinhVien>().Property(x => x.GioiTinh).HasConversion<string>();
            modelBuilder.Entity<Phong>().Property(x => x.TrangThai).HasConversion<string>();
            modelBuilder.Entity<Giuong>().Property(x => x.TrangThai).HasConversion<string>();
            modelBuilder.Entity<HopDong>().Property(x => x.TrangThai).HasConversion<string>();
            modelBuilder.Entity<Phi>().Property(x => x.LoaiPhi).HasConversion<string>();
            modelBuilder.Entity<Phi>().Property(x => x.TrangThaiThanhToan).HasConversion<string>();
            modelBuilder.Entity<PhanAnh>().Property(x => x.TrangThai).HasConversion<string>();
            modelBuilder.Entity<ViPham>().Property(x => x.MucDo).HasConversion<string>();

            // Ràng buộc duy nhất
            modelBuilder.Entity<NguoiDung>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<SinhVien>().HasIndex(x => x.MaSv).IsUnique();
            modelBuilder.Entity<SinhVien>().HasIndex(x => x.NguoiDungId).IsUnique();

            // Quan hệ + hành vi xóa
            modelBuilder.Entity<SinhVien>()
                .HasOne(sv => sv.NguoiDung)
                .WithOne(nd => nd.SinhVien)
                .HasForeignKey<SinhVien>(sv => sv.NguoiDungId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tang>()
                .HasOne(t => t.ToaNha)
                .WithMany(t => t.Tangs)
                .HasForeignKey(t => t.ToaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Phong>()
                .HasOne(p => p.Tang)
                .WithMany(t => t.Phongs)
                .HasForeignKey(p => p.TangId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Giuong>()
                .HasOne(g => g.Phong)
                .WithMany(p => p.Giuongs)
                .HasForeignKey(g => g.PhongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HopDong>()
                .HasOne(h => h.SinhVien)
                .WithMany(sv => sv.HopDongs)
                .HasForeignKey(h => h.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HopDong>()
                .HasOne(h => h.Giuong)
                .WithMany(g => g.HopDongs)
                .HasForeignKey(h => h.GiuongId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Phi>()
                .HasOne(p => p.HopDong)
                .WithMany(h => h.Phis)
                .HasForeignKey(p => p.HopDongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhanAnh>()
                .HasOne(pa => pa.SinhVien)
                .WithMany(sv => sv.PhanAnhs)
                .HasForeignKey(pa => pa.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhanAnh>()
                .HasOne(pa => pa.Phong)
                .WithMany(p => p.PhanAnhs)
                .HasForeignKey(pa => pa.PhongId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ViPham>()
                .HasOne(vp => vp.SinhVien)
                .WithMany(sv => sv.ViPhams)
                .HasForeignKey(vp => vp.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ViPham>()
                .HasOne(vp => vp.DieuKhoan)
                .WithMany(nq => nq.ViPhams)
                .HasForeignKey(vp => vp.DieuKhoanId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
