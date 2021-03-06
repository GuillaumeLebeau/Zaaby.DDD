﻿using System;
using System.Collections.Generic;
using System.Linq;
using OrderDomain.AggregateRoots;
using OrderDomain.IRepository;
using OrderRepository.PersistentObjects;
using Zaabee.Mongo.Abstractions;
using Zaabee.Redis.Abstractions;

namespace OrderRepository.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IZaabeeMongoClient _mongoClient;
        private readonly IZaabeeRedisClient _cache;

        public OrderRepository(IZaabeeMongoClient mongoClient,IZaabeeRedisClient cache)
        {
            _mongoClient = mongoClient;
            _cache = cache;
        }

        public void Add(Order order)
        {
            _mongoClient.Add(Convert(order));
            _cache.Add(order.Id, order);
        }

        public void Add(List<Order> orders)
        {
            _mongoClient.AddRange(Convert(orders));
            _cache.AddRange(orders.Select(order => new Tuple<string, Order>(order.Id, order)).ToList());
        }

        public bool Delete(Order order)
        {
            var result = _mongoClient.Delete(Convert(order)) > 0;
            _cache.Delete(order.Id);
            return result;
        }

        public int Delete(List<Order> orders)
        {
            var ids = orders.Select(p => p.Id).ToList();
            var result = (int) _mongoClient.Delete<OrderParentPo>(p => ids.Contains(p.Id));
            _cache.DeleteAll(ids);
            return result;
        }

        public bool Modify(Order order)
        {
            var result = _mongoClient.Update(Convert(order)) > 0;
            _cache.Add(order.Id, order);
            return result;
        }

        public int Modify(List<Order> orders)
        {
            var pos = Convert(orders);
            var result = 0;
            pos.ForEach(po => result += (int) _mongoClient.Update(po));
            _cache.AddRange(orders.Select(order => new Tuple<string, Order>(order.Id, order)).ToList());
            return result;
        }

        public Order Get(string id)
        {
            var order = _cache.Get<Order>(id);
            if (order != null) return order;
            order = Convert(_mongoClient.GetQueryable<OrderParentPo>().FirstOrDefault(p => p.Id == id));
            _cache.Add(order.Id, order);
            return order;
        }

        public List<Order> Get(List<string> ids)
        {
            var orders = _cache.Get<Order>(ids);
            var result = orders.Select(kv => kv.Value).ToList();
            if (orders.Count >= ids.Count) return result;

            var notFoundIds = ids.Where(id => !orders.ContainsKey(id)).ToList();
            var mongoOrders = Convert(_mongoClient.GetQueryable<OrderParentPo>()
                .Where(p => notFoundIds.Contains(p.Id)).ToList());
            if (mongoOrders.Count > 0)
                _cache.AddRange(mongoOrders.Select(order => new Tuple<string, Order>(order.Id, order)).ToList());
            result.AddRange(mongoOrders);

            return result;
        }

        public List<Order> GetAll()
        {
            return Convert(_mongoClient.GetQueryable<OrderParentPo>().ToList());
        }

        public List<Order> GetValidOrders()
        {
            throw new NotImplementedException();
        }

        public List<Order> GetOrdersByUser(Guid userId)
        {
            throw new NotImplementedException();
        }

        #region Convert

        private static List<OrderParentPo> Convert(List<Order> orders)
        {
            return new List<OrderParentPo>();
        }

        private static OrderParentPo Convert(Order order)
        {
            return new OrderParentPo { };
        }

        private static List<Order> Convert(List<OrderParentPo> orders)
        {
            return new List<Order>();
        }

        private static Order Convert(OrderParentPo order)
        {
            return null;
        }

        #endregion
    }
}