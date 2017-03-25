using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class PocoClass
    {
        public class Company
        {
            private readonly List<Employee> employees = new List<Employee>();
            public List<Employee> Employees { get { return employees;}}
        }

        public class Employee
        {
            public string EmployeeName {get;set;}
            public string Designation {get;set;}
        }
        [Test]
        public void CanSerializeCompany()
        {
            var model = TypeModel.Create();
            model.Add(typeof(Company), false).Add("Employees");
            model.Add(typeof(Employee), false).Add("EmployeeName", "Designation");
            model.CompileInPlace();

            Company comp = new Company {
                Employees = {
                    new Employee { Designation = "Boss", EmployeeName = "Fred"},
                    new Employee { Designation = "Grunt", EmployeeName = "Jo"},
                    new Employee { Designation = "Scapegoat", EmployeeName = "Alex"}}
            }, clone;
            using(var ms = new MemoryStream()) {
                model.Serialize(ms, comp);
                ms.Position = 0;
                Console.WriteLine("Bytes: " + ms.Length);
                clone = (Company) model.Deserialize(ms, null, typeof(Company));
            }
            Assert.AreEqual(3, clone.Employees.Count);
            Assert.AreEqual("Boss", clone.Employees[0].Designation);
            Assert.AreEqual("Alex", clone.Employees[2].EmployeeName);
        }
    }

    [TestFixture]
    public class PocoStruct
    {
        public struct Company
        {
            private List<Employee> employees;
            public List<Employee> Employees { get { return employees ?? (employees = new List<Employee>()); } }
        }

        public struct Employee
        {
            public string EmployeeName { get; set; }
            public string Designation { get; set; }
        }
        [Test]
        public void CanSerializeCompany()
        {
            var model = TypeModel.Create();
            model.Add(typeof(Company), false).Add("Employees");
            model.Add(typeof(Employee), false).Add("EmployeeName", "Designation");
            model.CompileInPlace();

            Company comp = new Company
            {
                Employees = {
                    new Employee { Designation = "Boss", EmployeeName = "Fred"},
                    new Employee { Designation = "Grunt", EmployeeName = "Jo"},
                    new Employee { Designation = "Scapegoat", EmployeeName = "Alex"}}
            }, clone;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, comp);
                ms.Position = 0;
                Console.WriteLine("Bytes: " + ms.Length);
                clone = (Company)model.Deserialize(ms, null, typeof(Company));
            }
            Assert.AreEqual(3, clone.Employees.Count);
            Assert.AreEqual("Boss", clone.Employees[0].Designation);
            Assert.AreEqual("Alex", clone.Employees[2].EmployeeName);
        }
    }
}
