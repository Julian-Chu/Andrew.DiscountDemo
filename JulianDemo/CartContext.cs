using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace JulianDemo
{
    public static class JulianDemo
    {
        static int _seed = 0;

        public static IEnumerable<Product> LoadProducts(string filename = @"products.json")
        {
            foreach (var p in JsonConvert.DeserializeObject<Product[]>(File.ReadAllText(filename)))
            {
                _seed++;
                p.Id = _seed;

                yield return p;
            }
        }

        public static IEnumerable<RuleBase> LoadRules()
        {
            //yield return new BuyMoreBoxesDiscountRule(2, 12);   // 買 2 箱，折扣 12%
            //yield return new TotalPriceDiscountRule(1000, 100); // 滿 1000 折 100
            //yield break;

            yield return new ComplexDiscountRule("同商品加購優惠", 10, "熱銷飲品", 12);
            yield return new DiscountRule4("同商品加購優惠", 10);
            yield return new DiscountRule6("熱銷飲品", 12);
        }
    }

    public class CartContext
    {
        public readonly List<Product> PurchasedItems = new List<Product>();
        public decimal TotalPrice = 0m;
    }

    public class Product
    {
        public int Id;
        public string SKU;
        public string Name;
        public decimal Price;
        public decimal Discount;
        public HashSet<string> Tags;
        public bool IsDiscounted = false;


        public string TagsValue
        {
            get
            {
                if (this.Tags == null || this.Tags.Count == 0) return "";
                return ", Tags: " + string.Join(",", this.Tags.Select(t => '#' + t));
            }
        }
    }

    public class POS
    {
        public readonly List<RuleBase> ActivedRules = new List<RuleBase>();

        public bool CheckoutProcess(CartContext cart)
        {
            // reset cart
            foreach (var rule in this.ActivedRules)
            {
                rule.Process(cart);
            }

            cart.TotalPrice = cart.PurchasedItems.Select(p => p.Price - p.Discount).Sum();
            return true;
        }
    }

    public abstract class RuleBase
    {
        public int Id;
        public string Name;
        public string Note;
        public abstract void Process(CartContext cart);
    }

    public class DiscountRule4 : RuleBase
    {
        private string TargetTag;
        private decimal BuyTwoGetOneSpecialPrice;

        public DiscountRule4(string tag, decimal amount)
        {
            this.Name = "同商品加購優惠";
            this.Note = $"加{amount}元多一件";
            this.TargetTag = tag;
            this.BuyTwoGetOneSpecialPrice = amount;
        }

        public override void Process(CartContext cart)
        {
            List<Product> matched = new List<Product>();
            foreach (var sku in cart.PurchasedItems.Where(p => p.Tags.Contains(this.TargetTag))
                .Select(p => p.SKU)
                .Distinct())
            {
                matched.Clear();
                foreach (var p in cart.PurchasedItems.Where(p => p.SKU == sku && !p.IsDiscounted))
                {
                    matched.Add(p);
                    if (matched.Count % 2 == 0)
                    {
                        matched.Last().Discount = matched.Last().Price - BuyTwoGetOneSpecialPrice;
                        matched.ForEach(p => p.IsDiscounted = true);
                        matched.Clear();
                    }
                }
            }
        }
    }

    public class DiscountRule6 : RuleBase
    {
        private string TargetTag;
        private int PercentOff;

        public DiscountRule6(string targetTag, int percentOff)
        {
            this.Name = "滿件折扣6";
            this.Note = $"滿{targetTag}二件結帳{10 - percentOff / 10}折";

            this.TargetTag = targetTag;
            this.PercentOff = percentOff;
        }

        public override void Process(CartContext cart)
        {
            List<Product> matched = new List<Product>();
            foreach (var p in cart.PurchasedItems.Where(p => p.Tags.Contains(this.TargetTag) && !p.IsDiscounted)
                .OrderByDescending(p => p.Price))
            {
                matched.Add(p);
                if (matched.Count == 2)
                {
                    foreach (var product in matched)
                    {
                        product.Discount = p.Price * this.PercentOff / 100;
                        product.IsDiscounted = true;
                    }

                    matched.Clear();
                }
            }
        }
    }

    public class ComplexDiscountRule : RuleBase
    {
        private int PercentOff;
        private decimal DiscountAmount;
        private string TagAmountDiscount;
        private string TagPercentOff;
        private string NoteTag1;
        private string NoteTag2;

        public ComplexDiscountRule(string tagAmountDiscount, decimal amount, string tagPercentOff, int percentOff)
        {
            this.TagAmountDiscount = tagAmountDiscount;
            this.NoteTag1 = $"滿{tagPercentOff}二件結帳{10 - percentOff / 10}折";
            this.DiscountAmount = amount;
            this.TagPercentOff = tagPercentOff;
            this.NoteTag2 = $"加{amount}元多一件";
            this.PercentOff = percentOff;
            this.Note = this.NoteTag1 + "," + NoteTag2;
        }


        public override void Process(CartContext cart)
        {
            var productsWithDoubleDiscount =
                cart.PurchasedItems.Where(p =>
                    p.Tags.Contains(TagAmountDiscount) && p.Tags.Contains(TagPercentOff) && !p.IsDiscounted).ToList();

            if (!productsWithDoubleDiscount.Any())
            {
                return;
            }

            List<Product> matched = new List<Product>();
            foreach (var sku in cart.PurchasedItems
                .Where(p => p.Tags.Contains(this.TagAmountDiscount) && !p.IsDiscounted)
                .Select(p => p.SKU)
                .Distinct())
            {
                matched.Clear();
                foreach (var p in cart.PurchasedItems.Where(p => p.SKU == sku))
                {
                    matched.Add(p);
                    if (matched.Count == 2)
                    {
                        matched.Last().Discount = matched.Last().Price - DiscountAmount;
                        matched.ForEach(m => m.IsDiscounted = true);
                        matched.Clear();
                    }
                }
            }

            foreach (var product in productsWithDoubleDiscount)
            {
                product.IsDiscounted = false;
            }

            var items = cart.PurchasedItems
                .Where(p => p.Tags.Contains(this.TagPercentOff) && !p.IsDiscounted)
                .OrderBy(p => p.Price - p.Discount);
            var count = items.Count() / 2;
            foreach (var product in items)
            {
                if (count > 0)
                {
                    product.Discount += (product.Price - product.Discount) * this.PercentOff / 100;
                    count--;
                }

                product.IsDiscounted = true;
            }
        }
    }
}