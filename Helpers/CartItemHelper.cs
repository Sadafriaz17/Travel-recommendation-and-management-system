using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Helpers
{
    public static class CartItemHelper
    {
        public static string GetItemName(Cart item)
        {
            if (item?.Product != null) return item.Product.ProductName;
            if (item?.Package != null) return item.Package.PackageName;
            return "Item";
        }

        public static string GetItemTypeName(Cart item)
        {
            if (item?.Product != null && item.Product.ProductType != null) return item.Product.ProductType.TypeName;
            if (item?.Package != null && item.Package.PackageType != null) return item.Package.PackageType.TypeName;
            return null;
        }

        public static string GetItemImage(Cart item)
        {
            if (item?.Product != null && !string.IsNullOrEmpty(item.Product.ImageUrl))
                return item.Product.ImageUrl;

            if (item?.Package != null && item.Package.PackageImages != null)
            {
                var image = item.Package.PackageImages.FirstOrDefault();
                if (image != null && !string.IsNullOrEmpty(image.ImageUrl))
                    return image.ImageUrl;
            }

            return null;
        }

        public static string GetItemImageUrl(UrlHelper url, Cart item)
        {
            var image = GetItemImage(item);
            return url.Content(image ?? string.Empty);
        }

        public static decimal GetLineTotal(Cart item)
        {
            if (item == null) return 0;
            return item.Price * item.Quantity;
        }
    }
}