using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebBan_1398.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Tổng tiền")]
        public decimal TotalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(250)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string Notes { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public List<OrderDetail> OrderDetails { get; set; } = new();
    }
}
