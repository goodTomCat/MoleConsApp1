using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MoleClientLib;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
//using JabyLibNetStandart.Other;
//using Microsoft.Extensions.DependencyModel;

namespace TestLib.ClientToClientTests
{
    public class ClientListner
    {
        //protected MoleClientCore Core;
        protected Task ListenTaskF;


        public ClientListner(Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> clientTupl, string dirForFileSaving, 
            MolePushServerSender serverSender, IPEndPoint endPoint, IEnumerable<CryptoFactoryBase> factories, MoleClientCore core)
        {
            if (clientTupl == null)
                throw new ArgumentNullException(nameof(clientTupl))
                { Source = GetType().AssemblyQualifiedName};
            if (!Directory.Exists(dirForFileSaving))
                throw new ArgumentException("Указанной директории для сохранения файлов не существует.", nameof(dirForFileSaving))
                { Source = GetType().AssemblyQualifiedName };
            if (serverSender == null)
                throw new ArgumentNullException(nameof(serverSender)) { Source = GetType().AssemblyQualifiedName };
            if (!serverSender.IsReg || !serverSender.IsAuth)
                throw new ArgumentException(
                    $"{nameof(serverSender)} не зарегистрировал или не авторизовал клиента на сервере.")
                { Source = GetType().AssemblyQualifiedName };
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint))
                { Source = GetType().AssemblyQualifiedName };
            if (factories == null)
                throw new ArgumentNullException(nameof(factories)) { Source = GetType().AssemblyQualifiedName };
            if (!factories.Any())
                throw new ArgumentException("factories.Any() == false.")
                { Source = GetType().AssemblyQualifiedName };

            Core = core;
            ClientFormTupl = clientTupl;
            FileSavingDirectory = dirForFileSaving;
            ServerSender = serverSender;
            EndPoint = endPoint;
            Factories = factories.ToList();
            var listner = new TcpListener(EndPoint);
            listner.Start();
            ListenTaskF = Task.Run(async () => await Listen(listner).ConfigureAwait(false));
            while (Core == null)
            {
                
            }
            PossibleCrypto = Core.PossibleAlgs;
            while (!ReadyToAcceptClient)
            {
                
            }
        }
        

        public IList<CryptoFactoryBase> Factories { get; protected set; }
        public bool ReadyToAcceptClient { get; protected set; }
        public MolePushServerSender ServerSender { get; protected set; }
        public string FileSavingDirectory { get; protected set; }
        public Tuple<UserForm, IEnumerable<TcpListener>, IAsymmetricKeysExchange> ClientFormTupl { get; protected set; }
        public List<MoleClientReceiver> Recievers { get; protected set; } = new List<MoleClientReceiver>();
        public CancellationTokenSource CancellationTokenSource { get; protected set; } = new CancellationTokenSource();
        public IPEndPoint EndPoint { get; protected set; }
        public IList<CryptoFactoryBase> CryptoFactories { get; protected set; }
        public PossibleCryptoInfo PossibleCrypto { get; protected set; }
        public MoleClientCore Core { get; protected set; }


        protected IList<CryptoFactoryBase> GetFactoriesFromAssemblies()
        {
            var dir = Directory.GetCurrentDirectory() + @"\Extensions";
            if (!Directory.Exists(dir))
                throw new InvalidOperationException($"Директории с крипто библиотеками не существует.")
                { Source = GetType().AssemblyQualifiedName };

            var conventions = new ConventionBuilder();
            conventions.ForTypesDerivedFrom<CryptoFactoryBase>().Export<CryptoFactoryBase>().Shared();
            string[] dllsLintInExtensions =
                Directory.GetFiles(dir, "*dll", SearchOption.TopDirectoryOnly).ToArray();
            var dllsFileInfo = dllsLintInExtensions.Select(s => new FileInfo(s)).ToArray();
            var ass = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies =
                dllsFileInfo.Select(s =>
                {
                    var first = ass.FirstOrDefault(name => s.Name.Contains(name.FullName));
                    if (first == null)
                        return Assembly.LoadFrom(s.FullName);
                    else
                        return first;
                }).ToArray();

            var configuration = new ContainerConfiguration().WithAssemblies(assemblies, conventions);
            using (var container = configuration.CreateContainer())
            {
                var factories = container.GetExports<CryptoFactoryBase>();
                return factories.ToArray();
            }

            //var dir = Directory.GetCurrentDirectory() + @"\Extensions";
            //if (!Directory.Exists(dir))
            //    throw new InvalidOperationException($"Директории с крипто библиотеками не существует.")
            //    { Source = GetType().AssemblyQualifiedName };

            //var conventions = new ConventionBuilder();
            //conventions.ForTypesDerivedFrom<CryptoFactoryBase>().Export<CryptoFactoryBase>().Shared();

            ////new DependencyContext()
            ////var ass = DependencyContext.Default.GetDefaultAssemblyNames();
            //string[] dllsLintInExtensions =
            //    Directory.GetFiles(dir, "*dll", SearchOption.TopDirectoryOnly).ToArray();
            //var dllsFileInfo = dllsLintInExtensions.Select(s => new FileInfo(s)).ToArray();
            //var assemblies =
            //    dllsFileInfo.Select(s =>
            //    {
            //        var first = ass.FirstOrDefault(name => s.Name.Contains(name.Name));
            //        if (first == null)
            //            return AssemblyLoadContext.Default.LoadFromAssemblyPath(s.FullName);
            //        else
            //            return AssemblyBuilder.Load(first);
            //    }).ToArray();

            //var configuration = new ContainerConfiguration().WithAssemblies(assemblies, conventions);
            //using (var container = configuration.CreateContainer())
            //{
            //    var factories = container.GetExports<CryptoFactoryBase>();
            //    CryptoFactories = factories.ToArray();
            //    return CryptoFactories;
            //}

            //var conventions = new ConventionBuilder();
            //conventions.ForTypesDerivedFrom<CryptoFactoryBase>().Export<CryptoFactoryBase>().Shared();

            //var ass = AppDomain.CurrentDomain.GetAssemblies();
            //var assemblies =
            //    dllsFileInfo.Select(s =>
            //    {
            //        var first = ass.FirstOrDefault(name => s.Name.Contains(name.FullName));
            //        if (first == null)
            //            return Assembly.LoadFrom(s.FullName);
            //        else
            //            return first;
            //    }).ToArray();

            //var configuration = new ContainerConfiguration().WithAssemblies(assemblies, conventions);
            //using (var container = configuration.CreateContainer())
            //{
            //    var factories = container.GetExports<CryptoFactoryBase>();
            //    return factories.ToArray();
            //}
        }
        protected MoleClientCore CreateCore()
        {
            return null;
            //if (Factories == null)
            //Factories = GetFactoriesFromAssemblies();
            //if (Core == null)
            //    Core = new MoleClientCore(new ProtoBufSerializer(), FileSavingDirectory, Factories, ClientFormTupl.Item1);
            //return Core;
        }
        protected async Task Listen(TcpListener listener)
        {
            try
            {
                //MoleClientCore core = CreateCore();
                var tasks = new List<Task>();
                while (true)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;
                    ReadyToAcceptClient = true;
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    var reciever = new MoleClientReceiver(client, Core, ServerSender);
                    Recievers.Add(reciever);
                    tasks.Add(reciever.RunAsync(CancellationTokenSource.Token));
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
}
