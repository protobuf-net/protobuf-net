using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.ServiceModel;

namespace Examples.Issues
{
    [TestFixture]
    public class SO10115538
    {
        public class MyService : IMyService
        {
            public string Test(Member member)
            {
                return "from svc: " + member.ToString();
            }
        }
        [ServiceContract]
        public interface IMyService
        {
            [OperationContract]
            string Test(Member member);
        }
        ServiceHost host;

        public IMyService GetService()
        {
            var endpoint =
                new ServiceEndpoint(
                ContractDescription.GetContract(typeof(IMyService)), new NetTcpBinding(SecurityMode.None),
                new EndpointAddress("net.tcp://localhost:89/MyService/svc"));
            endpoint.Behaviors.Add(new ProtoEndpointBehavior());
            return new ChannelFactory<IMyService>(endpoint).CreateChannel();
            
            //ChannelFactory<IMyService> factory = new ChannelFactory<IMyService>(new NetTcpBinding(SecurityMode.None), "net.tcp://localhost:89/MyService/svc");
            //var client = factory.CreateChannel();
            //return client;
        }
        [TestFixtureSetUp]
        public void StartServer()
        {
            try
            {
                StopServer();
                host = new ServiceHost(typeof(MyService),
                    new Uri("net.tcp://localhost:89/MyService"));
                host.AddServiceEndpoint(typeof (IMyService), new NetTcpBinding(SecurityMode.None),
                                        "net.tcp://localhost:89/MyService/svc").Behaviors.Add(
                                            new ProtoEndpointBehavior());
                host.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        [TestFixtureTearDown]
        public void StopServer()
        {
            if (host != null)
            {
                host.Close();
                host = null;
            }
        }


        Member InventMember()
        {
            Member m = new Member();
            m.FirstName = "Mike";
            m.LastName = "Hanrahan";
            m.UserId = new Guid("467c231f-f692-4432-ab1b-342c237b3ca9");
            m.AccountStatus = MemberAccountStatus.Blocked;
            m.EnteredBy = "qwertt";

            return m;
        }
        [Test]
        public void TestUsingMemoryStream()
        {
            Base.PrepareMetaDataForSerialization();
            var m = InventMember();
            Assert.AreEqual("Mike Hanrahan, 467c231f-f692-4432-ab1b-342c237b3ca9, Blocked, qwertt", m.ToString());
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<Member>(ms, m);
                Console.WriteLine(ms.Length.ToString());
                ms.Position = 0;
                var member2 = Serializer.Deserialize<Member>(ms);
                Assert.AreEqual("qwertt", member2.EnteredBy);
                Assert.AreEqual("Mike", member2.FirstName);

                Assert.AreEqual("Mike Hanrahan, 467c231f-f692-4432-ab1b-342c237b3ca9, Blocked, qwertt",
                                member2.ToString());
            }
        }


        [Test]
        public void TestUsingWcf()
        {
            Base.PrepareMetaDataForSerialization();
            var m = InventMember();
            var client = GetService();
            string s = client.Test(m);
            Assert.AreEqual("from svc: Mike Hanrahan, 467c231f-f692-4432-ab1b-342c237b3ca9, Blocked, qwertt", s);
        }

        /// <summary>
        /// This entity represents a member or user of the site.
        /// </summary>
        [DataContract]
        [Serializable]
        public class Member : User
        {
            public override string ToString()
            {
                return string.Format("{0} {1}, {2}, {3}, {4}", FirstName, LastName, UserId, AccountStatus, EnteredBy);
            }
            public Member()
                : base()
            {
                EntityType = EntityType.Member;
            }

            [DataMember(Order = 20)]
            public int Id { get; set; }

            [DataMember(Order = 21)]
            public string MemberName { get; set; }

            
            [DataMember(Order = 23)]
            public MemberAccountStatus AccountStatus { get; set; }

            #region static

            public static readonly string CacheCollectionKey = "MemberCollection";

            private static readonly string CacheItemKeyPrefix = "Member:";

            public static string GetCacheItemKey(int id)
            {
                return CacheItemKeyPrefix + id.ToString();
            }

            #endregion
        }
         /// <summary>
        /// This class represents a user in the system.  For example, a user could be a member or a merchant user.
        /// </summary>
        [DataContract]
        [Serializable]
        public class User: Base
        {
            public User()
                :base()
            {
                EntityType = EntityType.User;
            }

            [DataMember(Order = 10)]
            public Guid UserId { get; set; }

            [DataMember(Order = 11, Name = "First Name")]
            public string FirstName { get; set; }

            [DataMember(Order = 12, Name = "Last Name")]
            public string LastName { get; set; }

            }
        /// <summary>
        /// This is the base class for all entities involved in the request/response pattern of our services
        /// </summary>
        /// <remarks>
        /// The objects derived from this class are used to transfer data from our service classes to our UIs and back again and they should 
        /// not contain any logic.
        /// </remarks>
        [DataContract]
        [Serializable]
        public abstract class Base
        {
            public Base()
            {
                //Set some defaults for this
                EnteredBy = System.Environment.UserName;
                EnteredSource = System.Environment.MachineName;
            }

            /// <summary>
            /// This is the record timestamp
            /// </summary>
            [DataMember(Order = 2)]
            public DateTime RecordTimeStamp { get; set; }

            /// <summary>
            /// This is the name of the user who last edited the entity
            /// </summary>
            [DataMember(Order = 3)]
            public string EnteredBy { get; set; }

            /// <summary>
            /// This is the source of the last edited entity
            /// </summary>
            [DataMember(Order = 4)]
            public string EnteredSource { get; set; }

            /// <summary>
            /// Flag denoting if the record is a new record or not.
            /// </summary>
            /// <remarks>
            /// To flag an entity as an existing record call the "FlagAsExistingReport()" method.
            /// </remarks>
            public bool IsNewRecord
            {
                get
                {
                    return _isNewRecord;
                }
            }

            [DataMember(Order = 6)]
            protected bool _isNewRecord = true;
            /// <summary>
            /// Flags the entity as a record that already exists in the database
            /// </summary>
            /// <remarks>
            /// This is a method rather than a field to demonstrait that this should be called with caution (as opposed to inadvertantly setting a flag!)
            /// <para>
            /// Note that this method should only need to be called on object creation if the entity has a composite key.  Otherwise the flag is
            /// set when the id is being set.  It should always be called on saving an entity.
            /// </para>
            /// </remarks>
            public void FlagAsExistingRecord()
            {
                _isNewRecord = false;
            }


            /// <summary>
            /// This is the type of entity we are working with
            /// </summary>
            [DataMember(Order = 7)]
            private EntityType _entityType = EntityType.Unknown;
            public EntityType EntityType
            {
                get
                {
                    return _entityType;
                }
                protected set
                {
                    _entityType = value;
                }
            }


            /// <summary>
            /// Flag to say if the id generated for this class need to be int64 in size.
            /// </summary>
            [DataMember(Order = 9)]
            public bool IdRequiresInt64 { get; protected set; }

            /// <summary>
            /// This method tells us if the database id has been assigned.  Note that this does
            /// not mean the entity has been saved, only if the id has been assigned (so the id could be greater than 0, but the
            /// entity could still be a NewRecord
            /// </summary>
            /// <returns></returns>
            [DataMember(Order = 8)]
            public bool HasDbIdBeenAssigned { get; protected set; }

            private Guid _validationId = Guid.NewGuid();
            public Guid EntityValidationId
            {
                get
                {
                    return _validationId;
                }
            }

            /// <summary>
            /// Returns all known child types
            /// </summary>
            public IEnumerable<Type> GetAllTypes()
            {
                Assembly current = Assembly.GetCallingAssembly();
                List<Type> derivedTypes = new List<Type>();
                var allTypes = current.GetTypes();
                foreach (var t in allTypes)
                {
                    if (t.IsAssignableFrom(typeof(Base)))
                    {
                        derivedTypes.Add(t);
                    }
                }
                return derivedTypes;
            }

            #region Static Methods


            private static object _metaLock = new object();
            private static bool _metaDataPrepared = false;
            /// <summary>
            /// Creates protobuf type models from the entities in this assembly
            /// </summary>
            public static void PrepareMetaDataForSerialization()
            {
                lock (_metaLock)
                {
                    if (_metaDataPrepared) { return; }

                    Assembly current = Assembly.GetExecutingAssembly();
                    var allTypes = current.GetTypes();
                    foreach (var t in allTypes.Where(t => t.IsNested && t.DeclaringType == typeof(SO10115538)))
                    {
                        Console.WriteLine("Checking type: " + t.Name);
                        checkType(t);
                    }

                    _metaDataPrepared = true;
                }
            }

            private static void checkType(Type type)
            {
                Assembly current = Assembly.GetExecutingAssembly();
                var allTypes = current.GetTypes();
                int key = 1000;
                foreach (var t in allTypes)
                {
                    if (t.IsSubclassOf(type) && t.BaseType == type)
                    {
                        Console.WriteLine("Adding sub-type " + t.Name + " with key " + key);
                        RuntimeTypeModel.Default[type].AddSubType(key, t);
                        key++;
                    }
                }
            }

            #endregion
        }
        public enum EntityType
        {
            Unknown, Member, User
        }
        public enum MemberAccountStatus
        {
            Blocked
        }
        }

}
