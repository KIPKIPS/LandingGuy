﻿// author:KIPKIPS
// date:2023.02.03 22:02
// describe:工具类
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Framework {
    public static class Utils {
        #region File Load 文件加载
        
        /// <summary>
        /// 加载json文件
        /// </summary>
        /// <param name="path">加载路径</param>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>结果对象</returns>
        public static T LoadJsonByPath<T>(string path) {
            StreamReader reader = new StreamReader(Environment.CurrentDirectory + "/" + path);
            string jsonStr = reader.ReadToEnd();
            reader.Close();
            //字符串转换为对象
            return JsonConvert.DeserializeObject<T>(jsonStr);
        }
        #endregion

        #region Log 日志输出
        
        /// <summary>
        /// 日志实体
        /// </summary>
        private struct LogEntity : IPoolAble {
            public void OnRecycled() {
            }
            public bool IsRecycled { get; set; }
            public string content;
            public bool innerLine;
            public void Create(string _content, bool _innerLine) {
                content = _content;
                innerLine = _innerLine;
            }
        }

        // Log Color HashSet防止重复颜色   例如：<"log","#ff00ff">
        private static readonly Dictionary<string, string> _logColorDict = new ();
        private static readonly HashSet<string> _logColorHashSet = new ();
        private static readonly SimplePool<LogEntity> _logEntityPool = new ();
        private static readonly Dictionary<int, string> _spaceDict = new ();
        
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="messages">日志内容</param>
        public static void Log(params object[] messages) {
            string tag = "Log";
            if (messages == null || messages.Length == 0) {
                Debug.Log(GetLogFormatString(tag, "The expected value is null"));
                return;
            }
            int startIdx = 0;
            if (messages.Length == 1) {
                startIdx = 0;
            } else if (messages[0] is string) {
                tag = (string)messages[0];
                startIdx = 1;
            }
            string msg = "";
            LogEntity logEntity;
            for (int i = startIdx; i < messages.Length; i++) {
                logEntity = GetMessageData(messages[i]);
                msg += (logEntity.content + "\n");
                _logEntityPool.Recycle(logEntity);
            }
            Debug.Log(GetLogFormatString(tag, msg));
        }
        
        /// <summary>
        /// 获取日志数据
        /// </summary>
        /// <param name="msgObj">被打印的日志对象</param>
        /// <returns>日志实体</returns>
        private static LogEntity GetMessageData(object msgObj) => HandleLogUnit(true, msgObj, -1);
        
        /// <summary>
        /// 递归解析对象
        /// </summary>
        /// <param name="firstLine">是否第一行</param>
        /// <param name="msgObj">日志对象</param>
        /// <param name="layer">输出层级</param>
        /// <returns>日志实体</returns>
        private static LogEntity HandleLogUnit(bool firstLine, object msgObj, int layer) {
            string msg = "";
            bool innerLine = true;
            if (InheritInterface<IEnumerable>(msgObj) && !(msgObj is string)) {
                msg += firstLine ? "" : "\n";
                IEnumerator ie = ((IEnumerable)msgObj).GetEnumerator();
                string tempStr;
                int length = GetEnumeratorCount(ie);
                ie = ((IEnumerable)msgObj).GetEnumerator();
                int count = 0;
                bool last, isKvp;
                while (ie.MoveNext()) {
                    if (ie.Current != null) {
                        dynamic data = ie.Current;
                        isKvp = ContainProperty(data, "Key");
                        LogEntity le = HandleLogUnit(false, (isKvp ? data.Value : data), layer + 1);
                        last = count == length - 1;
                        tempStr = GetTable(layer + 1) + (isKvp ? data.Key : count) + " : " + le.content + (le.innerLine && !last ? "\n" : "");
                        innerLine = last | !innerLine;
                        msg += tempStr;
                    }
                    count++;
                }
            } else {
                msg = msgObj.ToString();
            }
            LogEntity logEntity = _logEntityPool.Allocate();
            logEntity.Create(msg, innerLine);
            return logEntity;
        }
        
        /// <summary>
        /// 获取迭代器长度
        /// </summary>
        /// <param name="ie">迭代器</param>
        /// <returns>迭代器长度</returns>
        private static int GetEnumeratorCount(IEnumerator ie) {
            int cnt = 0;
            while (ie.MoveNext()) {
                cnt++;
            }
            return cnt;
        }
        
        /// <summary>
        /// 格式化字符串
        /// </summary>
        /// <param name="num">层级</param>
        /// <returns>格式化后的字符串</returns>
        private static string GetTable(int num) {
            if (_spaceDict.ContainsKey(num)) {
                return _spaceDict[num];
            }
            string space = "";
            for (int i = 0; i < num; i++) {
                space += "    ";
            }
            _spaceDict.Add(num, space);
            return space;
        }
        
        /// <summary>
        /// 是否包含属性
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否包含该属性</returns>
        private static bool ContainProperty(object obj, string propertyName) => obj != null && !string.IsNullOrEmpty(propertyName) && obj.GetType().GetProperty(propertyName) != null;

        /// <summary>
        /// 是否继承接口
        /// </summary>
        /// <param name="obj">对象</param>
        /// <typeparam name="T">接口类型</typeparam>
        /// <returns>是否继承该接口</returns>
        private static bool InheritInterface<T>(object obj) => typeof(T).IsAssignableFrom(obj.GetType());

        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="message">日志对象</param>
        public static void LogWarning(object message) => LogWarning("WARNING", message);
        
        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="tag">前缀标签</param>
        /// <param name="message">日志对象</param>
        public static void LogWarning(string tag, object message) => Debug.LogWarning(GetLogFormatString(tag, message));

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="message">日志对象</param>
        public static void LogError(object message) => LogError("ERROR", message);
        
        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="tag">前缀标签</param>
        /// <param name="message">日志对象</param>
        public static void LogError(string tag, object message) => Debug.LogError(GetLogFormatString(tag, message));
        
        /// <summary>
        /// 获取格式化的日志信息
        /// </summary>
        /// <param name="tag">前缀标签</param>
        /// <param name="message">日志对象</param>
        /// <returns>格式化后的字符串</returns>
        private static string GetLogFormatString(string tag, object message) {
            string c;
            if (_logColorDict.ContainsKey(tag)) {
                c = _logColorDict[tag];
            } else {
                int count = 0; // 颜色循环次数上限
                do {
                    c = GetRandomColorCode();
                    count++;
                    if (count > 1000) {
                        // 获取颜色次数超过1000次 默认返回白色
                        Debug.LogWarning("Color Get Duplicated");
                        return $"<color=#000000>[{tag}]</color>: {message}";
                    }
                } while (_logColorHashSet.Contains(c));
                // 找到对应颜色
                _logColorDict[tag] = c;
                _logColorHashSet.Add(c);
            }
            return $"<color={c}>[{tag}]</color>: {message}";
        }

        #endregion

        #region Find 节点查找

        /// <summary>
        /// 查找对象
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="name">查找对象名称</param>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>查找结果</returns>
        public static T Find<T>(Transform root, string name) {
            if (name == null) {
                return root.GetComponent<T>();
            }
            Transform target = GetChild(root, name);
            return target != null ? target.GetComponent<T>() : default;
        }

        /// <summary>
        /// 查找对象
        /// </summary>
        /// <param name="name">查找的对象命名</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>查找的结果对象</returns>
        public static T Find<T>(string name) => GameObject.Find(name).transform.GetComponent<T>();

        /// <summary>
        /// 递归查找父节点下的对象
        /// </summary>
        /// <param name="root">起始节点名称</param>
        /// <param name="childName">子节点名称</param>
        /// <returns>查找到的子节点</returns>
        private static Transform GetChild(Transform root, string childName) {
            //根节点查找
            Transform childTrs = root.Find(childName);
            if (childTrs != null) {
                return childTrs;
            }
            //遍历子物体查找
            int count = root.childCount;
            for (int i = 0; i < count; i++) {
                childTrs = GetChild(root.GetChild(i), childName);
                if (childTrs != null) {
                    return childTrs;
                }
            }
            return null;
        }
        #endregion

        #region Color 颜色相关工具

        /// <summary>
        /// 文本着色
        /// </summary>
        /// <param name="str">着色字符串</param>
        /// <param name="hexColor">十六进制颜色</param>
        /// <returns>返回着色字符串</returns>
        public static string AddColor(string str, string hexColor) => $"<color=#{hexColor}>{str}</color>";
        static readonly System.Random random = new ();
        //color下划线颜色 line 线厚度
        // public static string AddUnderLine(string msg, int colorIndex, int line) {
        //     return string.Format("<UnderWave/color={0},thickness=${1}>{2}</UnderWave>", GetColor(colorIndex), line, msg);
        // }

        /// <summary>
        /// 获取随机颜色 默认alpha=1 
        /// </summary>
        /// <param name="isAlphaRandom">是否随机透明度</param>
        /// <returns>随机的颜色对象</returns>
        public static Color GetRandomColor(bool isAlphaRandom = false) => new (random.Next(255), random.Next(255), random.Next(255), isAlphaRandom ? random.Next() / 255 : 1);

        /// <summary>
        /// 获取随机颜色RGB A 例如：#ff00ff
        /// </summary>
        /// <returns>十六进制颜色字符串</returns>
        private static string GetRandomColorCode() => $"#{random.Next(255):X}{random.Next(255):X}{random.Next(255):X}";

        #endregion
    }
}