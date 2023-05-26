using FellowOakDicom;
using Microsoft.Extensions.Logging;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client.Advanced.Association;
using FellowOakDicom.Network.Client.Advanced.Connection;
using System.Text;
using System.Threading;

namespace BlockDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            ILogger<Program> logger = factory.CreateLogger<Program>();
            //create server, set max clients allowed
            using var server = DicomServerFactory.Create<CStoreSCP>(11112);
            server.Options.MaxClientsAllowed = 20;
            server.Logger = logger;
            //create connection request
            var connectionRequest = new AdvancedDicomClientConnectionRequest
            {
                NetworkStreamCreationOptions = new NetworkStreamCreationOptions
                {
                    Host = "127.0.0.1",
                    Port = server.Port,
                }
            };
            //create connection (this is already enough to force the problem, an association is not actually needed)
            using var connection = await AdvancedDicomClientConnectionFactory.OpenConnectionAsync(connectionRequest, default);

            //create an association
            var associationRequest = new AdvancedDicomClientAssociationRequest
            {
                CallingAE = "EchoSCU",
                CalledAE = "EchoSCP"
            };
            //open the association (but never close it)
            using var association = await connection.OpenAssociationAsync(associationRequest, default);

            while (true)
            {
                //do nothing
                await Task.Delay(10000);
            }
        }
    }
    public class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        private static readonly DicomTransferSyntax[] _acceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
        };

        private static readonly DicomTransferSyntax[] _acceptedImageTransferSyntaxes = new DicomTransferSyntax[]
        {
               // Lossless
               DicomTransferSyntax.JPEGLSLossless,
               DicomTransferSyntax.JPEG2000Lossless,
               DicomTransferSyntax.JPEGProcess14SV1,
               DicomTransferSyntax.JPEGProcess14,
               DicomTransferSyntax.RLELossless,
               // Lossy
               DicomTransferSyntax.JPEGLSNearLossless,
               DicomTransferSyntax.JPEG2000Lossy,
               DicomTransferSyntax.JPEGProcess1,
               DicomTransferSyntax.JPEGProcess2_4,
               // Uncompressed
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian,
               //Other
               DicomTransferSyntax.MPEG2,
               DicomTransferSyntax.MPEG2MainProfileHighLevel,
               DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41,
               DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For2DVideo,
               DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For3DVideo,
               DicomTransferSyntax.MPEG4AVCH264StereoHighProfileLevel42
        };


        public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding,ILogger log, DicomServiceDependencies dependencies)
            : base(stream, fallbackEncoding, log,dependencies)
        {
        }


        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            //if (_serviceConfig?.OnlyAcceptIfCalledAETIsCorrect ?? false)
            //{
            //    if (association.CalledAE != _serviceConfig.SCPAET)
            //    {
            //        return SendAssociationRejectAsync(
            //            DicomRejectResult.Permanent,
            //            DicomRejectSource.ServiceUser,
            //            DicomRejectReason.CalledAENotRecognized);
            //    }
            //}
            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification)
                {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    pc.AcceptTransferSyntaxes(_acceptedImageTransferSyntaxes);
                }
            }

            return SendAssociationAcceptAsync(association);
        }


        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }


        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            /* nothing to do here */
        }


        public void OnConnectionClosed(Exception exception)
        {
            /* nothing to do here */
        }


        public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            //if (_serviceConfig?.CreateBackups ?? false)
            //{
            //    var studyUid = request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID).Trim();
            //    var instUid = request.SOPInstanceUID.UID;

            //    var path = Path.GetFullPath(AppContext.BaseDirectory);
            //    path = Path.Combine(path, studyUid);

            //    if (!Directory.Exists(path))
            //    {
            //        Directory.CreateDirectory(path);
            //    }

            //    path = Path.Combine(path, instUid) + ".dcm";
            //    await request.File.SaveAsync(path);
            //}

            return new DicomCStoreResponse(request, DicomStatus.Success);
        }


        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            // let library handle logging and error response
            return Task.CompletedTask;
        }


        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }

    }
}