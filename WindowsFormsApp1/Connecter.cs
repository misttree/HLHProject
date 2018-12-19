using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
/*
 * 用于数据库处理的脚本：仅供测试使用
 */
namespace HLHApp
{
    class Connecter
    {
        private string constr = "server=localhost;User Id=root;password=;Database=hlhproject";
        private string query = "select * from test";
        private string delstr = "delete from  test";
        private string insert = "";
        private MySqlConnection myconnect;


        public Connecter()
        {
            myconnect = new MySqlConnection(constr);
        }

        // 执行查询函数
        public void SelectData()
        {
            myconnect.Open();
            MySqlCommand mycmd = new MySqlCommand(query, myconnect);
            MySqlDataReader mySqlDataReader = mycmd.ExecuteReader();
            string bookres = "";
            while (mySqlDataReader.Read() == true)
            {
                bookres = "\t";
                bookres += mySqlDataReader["testdate"];
                bookres += " \t";
                bookres += mySqlDataReader["value"];
                Console.WriteLine(bookres);
            }
            mySqlDataReader.Close();
            myconnect.Close();
        }

        // 执行插入函数
        public void InsertData(DateTime dateTime, double value)
        {
            myconnect.Open();
            insert = "insert into test(testdate, value) values ('";
            insert += dateTime.ToString();
            insert += "',";
            insert += value;
            insert += ");";
            MySqlCommand mySqlCommand = new MySqlCommand(insert, myconnect);
            mySqlCommand.ExecuteNonQuery();
            myconnect.Close();
        }

        //执行删除函数
        public void deleteData()
        {
            myconnect.Open();
            MySqlCommand mySqlCommand = new MySqlCommand(delstr, myconnect);
            mySqlCommand.ExecuteNonQuery();
            myconnect.Close();
        }
    }
}

