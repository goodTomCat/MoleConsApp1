using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using JabyLib;
using JabyLib.Other;
using MolePushServerLibPcl;
using SharedMoleRes.Client.Crypto;

//using System.Reflection.Emit;

namespace MoleChatTestsLib.MolePushServerTests
{
    public class PushServerListner
    {
        protected Task PushServerTaskF;
        protected UserFormContext DbContextF;
        protected List<MolePushServerReciever> RecieversF = new List<MolePushServerReciever>();
        protected CancellationTokenSource CancellationTokenSourceF = new CancellationTokenSource();


        public PushServerListner()
        {
            IPAddress[] localIPs = Dns.GetHostAddressesAsync(Dns.GetHostName()).Result;
            var ip = localIPs.First(address => address.AddressFamily == AddressFamily.InterNetwork);
            EndPoint = new IPEndPoint(ip, 19654);
            var tcp = new TcpListener(EndPoint);
            tcp.Start();
            DbContextF = new UserFormContext();
            PushServerTaskF = Task.Run(async () => await Listen(tcp).ConfigureAwait(false));

        }


        public UserFormContext DbContext => DbContextF;
        public List<MolePushServerReciever> Recievers => RecieversF;
        public CancellationTokenSource CancellationTokenSource => CancellationTokenSourceF;
        public IPEndPoint EndPoint { get; }
        public IList<CryptoFactoryBase> CryptoFactories { get; protected set; }
        public PossibleCryptoInfo PossibleCrypto { get; protected set; }


        protected IList<CryptoFactoryBase> GetFactoriesFromAssemblies()
        {
            var dir = Directory.GetCurrentDirectory() + @"\Extensions";
            if (!Directory.Exists(dir))
                throw new InvalidOperationException($"Директории с крипто библиотеками не существует.")
                    {Source = GetType().AssemblyQualifiedName};

            var conventions = new ConventionBuilder();
            conventions.ForTypesDerivedFrom<CryptoFactoryBase>().Export<CryptoFactoryBase>().Shared();

            var ass = DependencyContext.Default.GetDefaultAssemblyNames();
            string[] dllsLintInExtensions =
                Directory.GetFiles(dir, "*dll", SearchOption.TopDirectoryOnly).ToArray();
            var dllsFileInfo = dllsLintInExtensions.Select(s => new FileInfo(s)).ToArray();
            var assemblies =
                dllsFileInfo.Select(s =>
                {
                    var first = ass.FirstOrDefault(name => s.Name.Contains(name.Name));
                    if (first == null)
                        return AssemblyLoadContext.Default.LoadFromAssemblyPath(s.FullName);
                    else
                        return AssemblyBuilder.Load(first);
                    //var assName = new AssemblyName(s.FullName);
                    //if (first == null)
                    //    return AssemblyLoadContext.Default.LoadFromAssemblyPath(s.FullName);
                    //else
                    //    return Assembly.Load(first);
                }).ToArray();

            var configuration = new ContainerConfiguration().WithAssemblies(assemblies, conventions);
            using (var container = configuration.CreateContainer())
            {
                var factories = container.GetExports<CryptoFactoryBase>();
                CryptoFactories = factories.ToArray();
                return CryptoFactories;
            }
        }
        protected MolePushServerCore CreateCore()
        {
            var posCrypto = new PossibleCryptoInfo(new[] { "CngMicrosoft" }, new[] {"Sha256"}, new[] {"Rsa"}, new[] {"Aes"},
                new[] {"Ecc"});
            var factories = GetFactoriesFromAssemblies();
            var core = new MolePushServerCore(posCrypto, factories, DbContextF);
            PossibleCrypto = posCrypto;
            return core;
        }
        protected async Task Listen(TcpListener listener)
        {
            try
            {
                var core = CreateCore();
                var tasks = new List<Task>();
                while (true)
                {
                    if (CancellationTokenSourceF.IsCancellationRequested)
                        return;

                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    var reciever = new MolePushServerReciever(client, core, new ProtoBufSerializer());
                    RecieversF.Add(reciever);
                    tasks.Add(reciever.RunAsync(CancellationTokenSourceF.Token));
                }
            }
            catch (Exception ex)
            {
                
                throw;
            }
            
        }

    }
}
