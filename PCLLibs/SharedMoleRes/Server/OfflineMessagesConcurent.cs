using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SharedMoleRes.Server.Surrogates;

namespace SharedMoleRes.Server
{
    public class OfflineMessagesConcurent : IReadOnlyDictionary<string, IReadOnlyCollection<byte[]>>
    {
        //protected Dictionary<string, List<byte[]>> MessagesF =
        //    new Dictionary<string, List<byte[]>>();
        protected ConcurrentDictionary<string, ConcurrentBag<byte[]>> MessagesF =
            new ConcurrentDictionary<string, ConcurrentBag<byte[]>>();


        public OfflineMessagesConcurent(string receiverLogin)
        {
            ReceiverLogin = receiverLogin;
        }
        public OfflineMessagesConcurent(OfflineMessagesConcurent messages)
        {
            if (messages == null)
                return;

            ReceiverLogin = new string(ReceiverLogin.ToCharArray());
            MessagesF =
                new ConcurrentDictionary<string, ConcurrentBag<byte[]>>(MessagesF.ToDictionary(pair => pair.Key,
                    pair => pair.Value));
        }


        public string ReceiverLogin { get; }
        public int Count => MessagesF.Count;
        public IReadOnlyCollection<byte[]> this[string key]
        {
            get
            {
                ConcurrentBag<byte[]> collection;
                if (MessagesF.TryGetValue(key, out collection))
                    return collection;
                else
                    throw new KeyNotFoundException();
            }
        }
        public IEnumerable<string> Keys => MessagesF.Keys;
        public IEnumerable<IReadOnlyCollection<byte[]>> Values => MessagesF.Values;


        public bool ContainsKey(string key)
        {
            return MessagesF.ContainsKey(key);
        }
        public bool TryGetValue(string key, out IReadOnlyCollection<byte[]> value)
        {
            ConcurrentBag<byte[]> collection;
            if (MessagesF.TryGetValue(key, out collection))
            {
                value = collection;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
        public IEnumerator<KeyValuePair<string, IReadOnlyCollection<byte[]>>> GetEnumerator()
        {
            foreach (KeyValuePair<string, ConcurrentBag<byte[]>> pair in MessagesF)
            {
                yield return new KeyValuePair<string, IReadOnlyCollection<byte[]>>(pair.Key, pair.Value);
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <exception cref="ArgumentNullException">login == null. -or- message == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Длина строки не может равняться 0.</exception>
        public void Add(string login, byte[] message)
        {
            if (login == null)
                throw new ArgumentNullException(nameof(login));
            if (login.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(login.Length), "Длина строки не может равняться 0.");
            if (message == null)
                throw new ArgumentNullException(nameof(message));


            ConcurrentBag<byte[]> messagesCollection;
            while (true)
            {
                if (MessagesF.TryGetValue(login, out messagesCollection))
                {
                    messagesCollection.Add(message);
                    return;
                }
                else
                {
                    if (MessagesF.TryAdd(login, new ConcurrentBag<byte[]>(new[] { message })))
                        return;
                }
            }
            
        }
        /// <exception cref="ArgumentNullException">login == null. -or- messages == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Длина строки не может равняться 0.</exception>
        public void AddRange(string login, IEnumerable<byte[]> messages)
        {
            if (login == null)
                throw new ArgumentNullException(nameof(login));
            if (login.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(login.Length), "Длина строки не может равняться 0.");
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));


            ConcurrentBag<byte[]> messagesCollection;
            while (true)
            {
                if (MessagesF.TryGetValue(login, out messagesCollection))
                {
                    foreach (byte[] message in messages)
                        messagesCollection.Add(message);
                    return;
                }
                else
                {
                    if (MessagesF.TryAdd(login, new ConcurrentBag<byte[]>(messages)))
                        return;
                }
            }
        }
        /// <exception cref="ArgumentNullException">login == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Длина строки не может равняться 0.</exception>
        public bool TryTake(string login, out IEnumerable<byte[]> messages)
        {
            if (login == null)
                throw new ArgumentNullException(nameof(login));
            if (login.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(login.Length), "Длина строки не может равняться 0.");

            ConcurrentBag<byte[]> messagesCollection;
            if (MessagesF.TryRemove(login, out messagesCollection))
            {
                messages = messagesCollection;
                return true;
            }
            else
            {
                messages = null;
                return false;
            }
        }
        public IDictionary<string, IList<byte[]>> Take()
        {
            var dic = MessagesF.ToDictionary(
                pair => pair.Key,
                pair => (IList<byte[]>)new List<byte[]>(pair.Value));
            MessagesF.Clear();
            return dic;
        }
        public OfflineMessagesConcurent Clone(bool deepClone)
        {
            OfflineMessagesConcurent offlineMessagesConcurent;
            if (deepClone)
            {
                offlineMessagesConcurent = new OfflineMessagesConcurent(new string(ReceiverLogin.ToCharArray()))
                {
                    MessagesF =
                        new ConcurrentDictionary<string, ConcurrentBag<byte[]>>(MessagesF.ToDictionary(pair => pair.Key,
                            pair => pair.Value))
                };
            }
            else
            {
                offlineMessagesConcurent = new OfflineMessagesConcurent(ReceiverLogin) {MessagesF = MessagesF};
            }
            return offlineMessagesConcurent;
        }
        public static implicit operator OfflineMessagesConcurentSur(OfflineMessagesConcurent messages)
        {
            if (messages == null)
                return null;

            return new OfflineMessagesConcurentSur()
            {
                Messages =
                    new Dictionary<string, List<byte[]>>(messages.MessagesF.ToDictionary(pair => pair.Key,
                        pair => pair.Value.ToList())),
                ReceiverLogin = messages.ReceiverLogin
            };
        }
        public static implicit operator OfflineMessagesConcurent(OfflineMessagesConcurentSur sur)
        {
            if (sur == null)
                return null;
            if (sur.ReceiverLogin == null)
                return null;

            var mess = new OfflineMessagesConcurent(sur.ReceiverLogin);
            if (sur.Messages == null)
                return mess;

            mess.MessagesF =
                new ConcurrentDictionary<string, ConcurrentBag<byte[]>>(sur.Messages.ToDictionary(pair => pair.Key,
                    pair => new ConcurrentBag<byte[]>(pair.Value)));
            return mess;
        }



        //private UserForm _formOfSender;


        ///// <exception cref="ArgumentNullException">formOfSender == null.</exception>
        //public OfflineMessagesConcurent(UserForm formOfSender)
        //{
        //    if (formOfSender == null)
        //        throw new ArgumentNullException(nameof(formOfSender)) {Source = GetType().AssemblyQualifiedName};

        //    _formOfSender = formOfSender.GetUserPublicData();
        //}


        ///// <exception cref="ArgumentNullException">value == null.</exception>
        //public virtual UserForm FormOfSender
        //{
        //    get { return _formOfSender.GetUserPublicData(); }
        //    protected set
        //    {
        //        if (value == null)
        //            throw new ArgumentNullException(nameof(FormOfSender)) {Source = GetType().AssemblyQualifiedName};

        //        _formOfSender = value;
        //    }
        //}
        //public List<byte[]> Messages { get; } = new List<byte[]>();

    }
}
