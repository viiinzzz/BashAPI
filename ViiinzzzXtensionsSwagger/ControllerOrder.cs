//not working

/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Xtensions.Core.Swagger
{
    /*
    
    in Startup -> Configure -> add:

      app.UseSwaggerUI(c =>
            {
                c.OrderActionGroupsBy(
                    new ControllerOrderComparer<ApiController>(
                        Assembly.GetExecutingAssembly()));
            });

    then place order attribute before method declaration
    
        [ControllerOrder(2)] //for example

     * /
    public class ControllerOrderAttribute : Attribute
    {
        public int Order { get; private set; }

        public ControllerOrderAttribute(int order)
        {
            Order = order;
        }
    }


    public class ControllerOrderComparer<T> : IComparer<string>
    {
        private readonly Dictionary<string, int> _orders;

        public ControllerOrderComparer(Assembly assembly)
            : this(GetFromAssembly<T>(assembly)) { }

        public ControllerOrderComparer(IEnumerable<Type> controllers)
        {
            _orders = new Dictionary<string, int>(
                controllers.Where(c => c.GetCustomAttributes<ControllerOrderAttribute>().Any())
                .Select(c => new { Name = ResolveControllerName(c.Name), c.GetCustomAttribute<ControllerOrderAttribute>().Order })
                .ToDictionary(v => v.Name, v => v.Order), StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<Type> GetFromAssembly<TController>(Assembly assembly)
        {
            return assembly.GetTypes().Where(c => typeof(TController).IsAssignableFrom(c));
        }

        public int Compare(string controllerX, string controllerY)
        {
            if (!_orders.TryGetValue(controllerX, out int xOrder))
                xOrder = int.MaxValue;
            if (!_orders.TryGetValue(controllerY, out int yOrder))
                yOrder = int.MaxValue;

            if (xOrder != yOrder)
                return xOrder.CompareTo(yOrder);

            return string.Compare(controllerX, controllerY, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveControllerName(string name)
        {
            const string suffix = "Controller";

            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - suffix.Length);

            return name;
        }
    }


}
*/