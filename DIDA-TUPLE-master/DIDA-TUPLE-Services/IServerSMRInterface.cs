using System;
using System.Collections.Generic;

namespace DTServices
{
    public interface IServerInterface
    {
        IDIDATuple Add(IDIDATuple fields);
        IDIDATuple Read(IDIDATuple fields, string name, int port);
        IDIDATuple Take(IDIDATuple fields, string name, int port);
        void Wait(int mil);
        void Crash();
        void Freeze();
        void Unfreeze();
        string Server( string url, string min, string max);

        bool XLBroadcastAdd(IDIDATuple didaTuple);
        IDIDATuple XLBroadcastRead(IDIDATuple didaTuple);
        List<IDIDATuple> XLBroadcastTake(IDIDATuple didaTuple);
        bool XLBroadcastRemove(IDIDATuple didaTuple);

        int GetNumOps();
        List<IDIDATuple> GetTupleSpace();

    }

    public interface IPuppetMasterInterface
    {
        void Server(string server_id, string url, string min, string max);
        string Client(string client_id, string url, string path_to_script);
        void Status();
        void Crash(string url);
        void Freeze(string url);
        void Unfreeze(string url);
        void Wait( int mil);
    }

    public interface IDIDATuple { }

    public interface IClientSMRInterface
    {
        void ContinueCommand();
    }

    public interface IServerXLInterface
    {
        IDIDATuple Add(IDIDATuple fields);
        IDIDATuple Read(IDIDATuple fields, string name, int port);
        IDIDATuple Take(IDIDATuple fields, string name, int port);
        void Wait(int mil);
        void Crash();
        void Freeze();
        void Unfreeze();
        string Server(string url, string min, string max);
        bool XLBroadcast(String command, IDIDATuple didaTuple);

    }
}
