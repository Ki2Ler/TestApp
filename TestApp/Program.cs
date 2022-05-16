using System;
using System.Data.Odbc;
using System.IO;

namespace TestApp
{
    internal class Program
    {
        static OdbcConnection odbcCsv;
        static void Main(string[] args)
        {
            //получаю папку с программой, в каталоге должен быть каталог с файлами
            string mPath = Environment.CurrentDirectory;
            string dbFolder = string.Format(@"{0}\{1}", mPath, "DB");
            string strConn = "Driver={Microsoft Text Driver (*.txt; *.csv)};Dbq=" + dbFolder + @"\;Extensions=asc,csv,tab,txt;";
            //проверю что файлы с данными на месте
            if ((File.Exists(string.Format(@"{0}\{1}", dbFolder, "department.csv")) == false) || (File.Exists(string.Format(@"{0}\{1}", dbFolder, "employee.csv")) == false))
            {
                Console.WriteLine("Не найдены файлы с данными в каталоге DB (department.csv, employee.csv)");
                Console.ReadKey();
                return;
            }
            try
            {
                //пробую подключиться
                odbcCsv = new OdbcConnection(strConn); 
                odbcCsv.Open();
            }
            catch (Exception err)
            {
                Console.WriteLine("Произошла ошибка подключения к данным, проверьте правильности пути. Ошибка {0}", err.ToString());
                Console.ReadKey();
                return;
            }
            int userChoiceNum = 0;
            OdbcCommand oCmd;
            OdbcDataReader oDR;
            do
            {
                userChoiceNum = Menu("Выберите задачу", new string[] { "Суммарную зарплату в разрезе департаментов", "Департамент, в котором у сотрудника зарплата максимальна", "Зарплаты руководителей департаментов (по убыванию)" });
                switch (userChoiceNum)
                {
                    case 1:
                        int userConditionChoice = Menu("Выберите условия", new string[] { "С руководителями", "Без руководителей" });
                        if (userConditionChoice == -1)
                            continue;
                        //для подсчета сумм зарплат в разрезе департментов использую встроенную функцию SUM, с группировкой по полю department_id. Название департамента джойню из таблицы department по полю id и department_id
                        if (userConditionChoice == 1)
                            oCmd = new OdbcCommand("SELECT [employee#csv].[department_id], [department#csv].[name], SUM([employee#csv].[salary]) FROM employee.csv LEFT JOIN department.csv ON [department#csv].[id] = [employee#csv].[department_id] GROUP BY [employee#csv].[department_id], [department#csv].[name]", odbcCsv);
                        else //чтобы исключить руководителей добавляю условие id сотрудника не должен быть в поле chief_id. Считаю что chief_id NULL только у главного босса, на случай если он не будет являться никому руководителем (?), то дополняю условие поле chief_id не должно быть NULL
                            oCmd = new OdbcCommand("SELECT [employee#csv].[department_id], [department#csv].[name], SUM([employee#csv].[salary]) FROM employee.csv LEFT JOIN department.csv ON [department#csv].[id] = [employee#csv].[department_id] WHERE [employee#csv].[id] NOT IN (SELECT DISTINCT [employee#csv].[chief_id] FROM employee.csv WHERE [employee#csv].[chief_id] IS NOT NULL) AND [employee#csv].[chief_id] IS NOT NULL GROUP BY [employee#csv].[department_id], [department#csv].[name]", odbcCsv);
                        oDR = oCmd.ExecuteReader();
                        while (oDR.Read())
                        {
                            Console.WriteLine("Суммарная зарплата в департаменте {0} = {1}", oDR[1], oDR[2]);
                        }
                        Console.ReadKey();
                        break;
                    case 2:
                        //Для поиска максмимальной зарплаты использую встроенную функцию MAX. Нахожу максимальную зарплату среди сотрудников и вывожу все уникальные департаменты сотрудников у кого зарплата совпадает с максимумом
                        oCmd = new OdbcCommand("SELECT DISTINCT [employee#csv].[department_id], [department#csv].[name] FROM employee.csv LEFT JOIN department.csv ON [department#csv].[id] = [employee#csv].[department_id] WHERE [employee#csv].[salary] = (SELECT MAX([employee#csv].[salary]) FROM employee.csv)", odbcCsv);
                        oDR = oCmd.ExecuteReader();
                        while (oDR.Read())
                        {
                            Console.WriteLine("Максимальная зарплата в департаменте {0}", oDR[1]);
                        }
                        Console.ReadKey();
                        break;
                    case 3:
                        //для выврода суммы зарплат руководителей вывожу сумму зарплаты и имя сотрудника с условием что id сотрудника присутствует в поле chief_id или для главного босса поле chief_id = NULL 
                        oCmd = new OdbcCommand("SELECT [employee#csv].[salary], [employee#csv].[name] FROM employee.csv WHERE [employee#csv].[id] IN (SELECT DISTINCT [employee#csv].[chief_id] FROM employee.csv WHERE [employee#csv].[chief_id] IS NOT NULL) OR  [employee#csv].[chief_id] IS NULL ORDER BY [employee#csv].[salary] DESC", odbcCsv);
                        oDR = oCmd.ExecuteReader();
                        while (oDR.Read())
                        {
                            Console.WriteLine("Зарплата руководителя департамента {0} = {1}", oDR[1], oDR[0]);
                        }
                        Console.ReadKey();
                        break;
                }
                Console.WriteLine("Нажмите любую клавишу для возврата");
            }
            while (userChoiceNum != -1);
            odbcCsv.Close();
        }
        //реализуем отдельный метод для запроса параметров у пользователя
        static int Menu(string menuTitle, string[] menuItems)
        {
            Console.Clear();
            Console.WriteLine("{0}:", menuTitle);
            for (int i = 0; i < menuItems.Length; i++)
            {
                Console.WriteLine("{0}. {1}", i + 1, menuItems[i]);
            }
            Console.WriteLine();
            Console.WriteLine("Нажмите клавишу ESC Для выхода");
            ConsoleKeyInfo userChoice;
            int taskNumber = 0;
            do
            {
                userChoice = Console.ReadKey();
                if (char.IsDigit(userChoice.KeyChar))
                {
                    taskNumber = int.Parse(userChoice.KeyChar.ToString());
                }
                else
                {
                    if (userChoice.Key != ConsoleKey.Escape)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Введите число от 1 до {0}, для выхода Escape", menuItems.Length);
                    }
                    continue;
                }
            } while ((userChoice.Key != ConsoleKey.Escape) && (taskNumber < 1 || taskNumber > menuItems.Length)); //выход по клавише ESC или верно веденной цийры пункта меню
            Console.WriteLine();
            if (userChoice.Key == ConsoleKey.Escape)
                taskNumber = -1;
            return taskNumber;
        }
    }
}
