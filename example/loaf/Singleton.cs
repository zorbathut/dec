
namespace Loaf
{
    public class SingletonManual<T> where T : class, new()
    {
        private static T instance = null;
        public static T Instance
        {
            get
            {
                return instance;
            }
        }

        public static void Set(T obj)
        {
            instance = obj;
        }
    }
}
