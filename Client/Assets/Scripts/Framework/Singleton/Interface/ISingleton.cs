// author:KIPKIPS
// date:2023.02.02 22:17
// describe:单例接口,实现单例的类都需要实现该接口
namespace Framework.Singleton {
    public interface ISingleton {
        /// <summary>
        /// 初始化函数
        /// </summary>
        void Initialize();
    }
}