﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace TMDT.Logic
{
    public partial class ShoppingCart
    {
        TMDTModel storeDB = new TMDTModel();


        string ShoppingCartId { get; set; }
        public const string CartSessionKey = "CartID";
        public static ShoppingCart GetCart(HttpContextBase context, string username)
        {
            var cart = new ShoppingCart();
            if (String.IsNullOrEmpty(username))
                cart.ShoppingCartId = cart.GetCartId(context);
            else
                cart.ShoppingCartId = cart.GetCartId(context, username);
            return cart;
        }
        // Helper method to simplify shopping cart calls
        public void AddToCart(Product album)
        {
            Random rand = new Random();
            // Get the matching cart and album instances
            var cartItem = storeDB.ShoppingCartItems.SingleOrDefault(
                c => c.CartID == ShoppingCartId
                && c.ProductID == album.ProductID);

            if (cartItem == null)
            {
                // Create a new cart item if no cart item exists
                cartItem = new cartitem
                {
                    ItemID = rand.Next().ToString(),
                    ProductID = album.ProductID,
                    CartID = ShoppingCartId,
                    Quantity = 1,
                    DateCreated = DateTime.Now
                };
                storeDB.ShoppingCartItems.Add(cartItem);
            }
            else
            {
                // If the item does exist in the cart, 
                // then add one to the quantity
                cartItem.Quantity++;
            }
            // Save changes
            storeDB.SaveChanges();
        }
        public void UpdateCart(List<cartitem> cart)
        {
            try
            {
                EmptyCart();
                for (int i = 0; i < cart.Count; i++)
                {
                    cartitem cartitem = new cartitem
                    {
                        ItemID = cart[i].ItemID,
                        Quantity = cart[i].Quantity,
                        ProductID = cart[i].ProductID,
                        CartID = cart[i].CartID,
                        DateCreated = cart[i].DateCreated
                    };
                    storeDB.ShoppingCartItems.Add(cartitem);
                }
                storeDB.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
        }
        public int? RemoveFromCart(int id)
        {
            // Get the cart
            var cartItem = storeDB.ShoppingCartItems.Single(
                cart => cart.CartID.Equals(ShoppingCartId)
                && cart.ItemID.Equals(id.ToString()));

            int? itemCount = 0;

            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                    itemCount = cartItem.Quantity;
                }
                else
                {
                    storeDB.ShoppingCartItems.Remove(cartItem);
                }
                // Save changes
                storeDB.SaveChanges();
            }
            return itemCount;
        }
        public void EmptyCart()
        {
            var cartItems = storeDB.ShoppingCartItems.Where(
                cart => cart.CartID == ShoppingCartId);

            foreach (var cartItem in cartItems)
            {
                storeDB.ShoppingCartItems.Remove(cartItem);
            }
            // Save changes
            storeDB.SaveChanges();
        }
        public List<cartitem> GetCartItems()
        {
            return storeDB.ShoppingCartItems.Where(
                cart => cart.CartID == ShoppingCartId).ToList();
        }
        public int GetCount()
        {
            // Get the count of each item in the cart and sum them up
            int? count = (from cartItems in storeDB.ShoppingCartItems
                          where cartItems.CartID == ShoppingCartId
                          select (int?)cartItems.Quantity).Sum();
            // Return 0 if all entries are null
            return count ?? 0;
        }
        public decimal GetTotal()
        {
            // Multiply album price by count of that album to get 
            // the current price for each of those albums in the cart
            // sum all album price totals to get the cart total
            decimal? total = (from cartItems in storeDB.ShoppingCartItems
                              where cartItems.CartID == ShoppingCartId
                              select (int?)cartItems.Quantity *
                              cartItems.Product.Price).Sum();

            return total ?? decimal.Zero;
        }

        //public int CreateOrder(Order order)
        //{
        //    decimal orderTotal = 0;

        //    var cartItems = GetCartItems();
        //    // Iterate over the items in the cart, 
        //    // adding the order details for each
        //    foreach (var item in cartItems)
        //    {
        //        var orderDetail = new OrderDetail
        //        {
        //            AlbumId = item.AlbumId,
        //            OrderId = order.OrderId,
        //            UnitPrice = item.Album.Price,
        //            Quantity = item.Count
        //        };
        //        // Set the order total of the shopping cart
        //        orderTotal += (item.Count * item.Album.Price);

        //        storeDB.OrderDetails.Add(orderDetail);

        //    }
        //    // Set the order's total to the orderTotal count
        //    order.Total = orderTotal;

        //    // Save the order
        //    storeDB.SaveChanges();
        //    // Empty the shopping cart
        //    EmptyCart();
        //    // Return the OrderId as the confirmation number
        //    return order.OrderId;
        //}
        //// We're using HttpContextBase to allow access to cookies.
        public string GetCartId(HttpContextBase context, string username)
        {
            if (context.Session[CartSessionKey] == null)
            {
                context.Session[CartSessionKey] = username;
            }
            return context.Session[CartSessionKey].ToString();
        }
        public string GetCartId(HttpContextBase context)
        {
            if (context.Session[CartSessionKey] == null)
            {
                // Generate a new random GUID using System.Guid class
                Guid tempCartId = Guid.NewGuid();
                // Send tempCartId back to client as a cookie
                context.Session[CartSessionKey] = tempCartId;
            }
            return context.Session[CartSessionKey].ToString();
        }
        //// When a user has logged in, migrate their shopping cart to
        //// be associated with their username
        public void MigrateCart(string userName)
        {
            var shoppingCart = storeDB.ShoppingCartItems.Where(
                c => c.CartID == ShoppingCartId);

            foreach (cartitem item in shoppingCart)
            {
                item.CartID = userName;
            }
            storeDB.SaveChanges();

            var cart = storeDB.ShoppingCartItems.Where(
                c => c.CartID == userName).ToList();
            int i = 0;
            if (cart.Count > 1)
            {
                foreach (cartitem item in cart)
                {
                    int id = item.ProductID;
                    foreach (cartitem itemrm in cart.Where(s => s.ProductID == id))
                    {
                        if (i != 0)
                        {
                            item.Quantity += itemrm.Quantity;
                            storeDB.ShoppingCartItems.Remove(itemrm);
                        }
                        i = 1;

                    }

                    i = 0;
                }

            }
            storeDB.SaveChanges();
        }
    }
}