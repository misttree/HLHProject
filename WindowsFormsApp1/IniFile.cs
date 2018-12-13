using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace INI
{
    public class IniFile
    {
        public string path;     //INI文件名

        //声明读写INI文件的API函数     
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFile(string INIPath)
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
            path = INIPath;
        }

        /// <summary>
        /// 写INI文件
        /// </summary>
        /// <param name="Section">节(section)</param>
        /// <param name="Key">关键词(key)</param>
        /// <param name="Value">值(Value)</param>
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>
        /// 读取INI文件
        /// </summary>
        /// <param name="Section">节(section)</param>
        /// <param name="Key">关键词(key)</param>
        /// <returns>值(Value)</returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);
            return temp.ToString();
        }

    }
}
