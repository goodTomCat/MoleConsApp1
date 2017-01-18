namespace SharedMoleRes.Server.Surrogates
{
    //[ProtoContract]
    //public class OfflineMessagesSur
    //{
    //    [ProtoMember(1)]
    //    public UserForm FormOfSender { get; set; }
    //    [ProtoMember(2)]
    //    public List<byte[]> Messages { get; set; }


    //    public static implicit operator OfflineMessagesConcurent(OfflineMessagesSur sur)
    //    {
    //        if (sur?.FormOfSender == null)
    //            return null;

    //        var mes = new OfflineMessagesConcurent(sur.FormOfSender);
    //        if (sur.Messages != null && sur.Messages.Count > 0)
    //            mes.Messages.AddRange(sur.Messages);
    //        return mes;
    //    }

    //    public static implicit operator OfflineMessagesSur(OfflineMessagesConcurent mes)
    //    {
    //        if (mes == null)
    //            return null;

    //        return new OfflineMessagesSur() {FormOfSender = mes.FormOfSender, Messages = mes.Messages};
    //    }
    //}
}
