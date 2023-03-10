// author:KIPKIPS
// date:2023.02.02 22:23
// describe:普通单例属性
namespace Framework.Singleton {
    /// <summary>
    /// 属性单例类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class SingletonProperty<T> where T : class, ISingleton {
        /// <summary>
        /// 静态实例
        /// </summary>
        private static T _instance;

        /// <summary>
        /// 标签锁
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new ();

        /// <summary>
        /// 静态属性
        /// </summary>
        public static T Instance {
            get {
                lock (Lock) {
                    _instance ??= SingletonCreator.CreateSingleton<T>();
                }
                return _instance;
            }
        }
        /// <summary>
        /// 资源释放
        /// </summary>
        public static void Dispose() => _instance = null;
    }
}