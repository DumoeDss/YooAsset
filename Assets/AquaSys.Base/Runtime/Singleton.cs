using UnityEngine;
namespace AquaSys.Base
{

    // 普通单例
    public abstract class Singleton<T>
        where T : new()
    {
        private static T _instance;
        private static object _lock = new object();
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 上锁，防止重复实例化
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                        }
                    }
                }
                return _instance;
            }
        }
    }

    // 组件单例 不可销毁 不存在时会通过反射创建
    public class UnitySingleton<T> : MonoBehaviour
        where T : Component
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 利用反射创建 Unity 物体
                    _instance = FindObjectOfType(typeof(T)) as T;
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        // 利用反射创建 Unity 组件
                        _instance = obj.AddComponent(typeof(T)) as T;
                    }
                }
                return _instance;
            }
        }
        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        protected virtual void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            if (_instance == null)
            {
                _instance = this as T;
            }
            else
            {
                if (_instance != this as T)
                    Destroy(this.gameObject);
            }
        }
    }

    // 组件单例 可销毁  不存在时会通过反射创建
    public class GameSingleton<T> : MonoBehaviour
        where T : Component
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 利用反射创建 Unity 物体
                    _instance = FindObjectOfType(typeof(T)) as T;
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        // 利用反射创建 Unity 组件
                        _instance = obj.AddComponent(typeof(T)) as T;
                    }
                }
                return _instance;
            }
        }
        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else
            {
                if (_instance != this as T)
                    Destroy(this.gameObject);
            }
        }
    }

    // 组件单例 可销毁
    public class SimpleGameSingleton<T> : MonoBehaviour
        where T : Component
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<T>();

                return _instance;
            }
        }
        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else
            {
                if (_instance != this as T)
                    Destroy(this.gameObject);
            }
        }
    }

    // 组件单例 不可销毁
    public class SimpleUnitySingleton<T> : MonoBehaviour
       where T : Component
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<T>();
                return _instance;
            }
        }
        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        protected virtual void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            if (_instance == null)
            {
                _instance = this as T;
            }
            else
            {
                if (_instance != this as T)
                    Destroy(this.gameObject);
            }
        }
    }
}
