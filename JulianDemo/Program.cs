using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace JulianDemo
{
    public class Program
    {
        
        static void Main(string[] args)
        {
            CartContext cart = new CartContext();
            POS pos = new POS();
            cart.PurchasedItems.AddRange(LoadProducts(@"..\..\..\products4.json"));
            pos.ActivedRules.AddRange(LoadRules());
            pos.CheckoutProcess(cart);
            // CartContext cart = new CartContext();
            // POS pos = new POS();
            //
            // cart.PurchasedItems.AddRange(LoadProducts(@"..\..\..\products3.json"));
            // pos.ActivedRules.AddRange(LoadRules());
            //
            // pos.CheckoutProcess(cart);
            //
            Console.WriteLine($"購買商品:");
            Console.WriteLine($"---------------------------------------------------");
            foreach(var p in cart.PurchasedItems)
            {
                Console.WriteLine($"- {p.Id,02}, [{p.SKU}] {p.Price,8:C}, {p.Name} 折扣{p.Discount} {p.TagsValue}");
            }
            Console.WriteLine();
            
            Console.WriteLine();
            // Console.WriteLine($"折扣:");
            // Console.WriteLine($"---------------------------------------------------");
            // foreach(var d in cart.AppliedDiscounts)
            // {
            //     Console.WriteLine($"- 折抵 {d.Amount,8:C}, {d.Rule.Name} ({d.Rule.Note})");
            //     foreach (var p in d.Products) Console.WriteLine($"  * 符合: {p.Id, 02}, [{p.SKU}], {p.Name} {p.TagsValue}");
            //     Console.WriteLine();
            // }
            Console.WriteLine();
            
            Console.WriteLine($"---------------------------------------------------");
            Console.WriteLine($"結帳金額:   {cart.TotalPrice:C}");
        }
        
        
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
}