using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using ProtoBuf.Meta;
using Xunit.Abstractions;

namespace ProtoBuf.unittest.Meta
{   
    public class PocoClass
    {
        private ITestOutputHelper Log { get; }
        public PocoClass(ITestOutputHelper _log) => Log = _log;

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
        [Fact]
        public void CanSerializeCompany()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Company), false).Add("Employees");
            model.Add(typeof(Employee), false).Add("EmployeeName", "Designation");
            model.CompileInPlace();

            Company comp = new Company {
                Employees = {
                    new Employee { Designation = "Boss", EmployeeName = "Fred"},
                    new Employee { Designation = "Grunt", EmployeeName = "Jo"},
                    new Employee { Designation = "Scapegoat", EmployeeName = "Alex"}}
            }, clone;
            using var ms = new MemoryStream();
            model.Serialize(ms, comp);
            ms.Position = 0;
            Log.WriteLine("Bytes: " + ms.Length);
#pragma warning disable CS0618
            clone = (Company) model.Deserialize(ms, null, typeof(Company));
#pragma warning restore CS0618
            Assert.Equal(3, clone.Employees.Count);
            Assert.Equal("Boss", clone.Employees[0].Designation);
            Assert.Equal("Alex", clone.Employees[2].EmployeeName);
        }
    }

    
    public class PocoStruct
    {
        private ITestOutputHelper Log { get; }
        public PocoStruct(ITestOutputHelper _log) => Log = _log;

        public struct Company
        {
            private List<Employee> employees;
            public List<Employee> Employees => employees ??= new List<Employee>();
        }

        public struct Employee
        {
            public string EmployeeName { get; set; }
            public string Designation { get; set; }
        }
        [Fact]
        public void CanSerializeCompany()
        {
            var model = RuntimeTypeModel.Create();
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
                Log.WriteLine("Bytes: " + ms.Length);
#pragma warning disable CS0618
                clone = (Company)model.Deserialize(ms, null, typeof(Company));
#pragma warning restore CS0618
            }
            Assert.Equal(3, clone.Employees.Count);
            Assert.Equal("Boss", clone.Employees[0].Designation);
            Assert.Equal("Alex", clone.Employees[2].EmployeeName);
        }
    }
}
