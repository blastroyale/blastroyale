using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Game.Logic
{
    /// <summary>
    /// This logic provides the necessary behaviour to manage the player's content creator data
    /// Player can be a content creator and/or support one content creator as well.
    /// </summary>
    public interface IContentCreatorDataProvider
    {
        /// <summary>
        /// Requests the code of who this player is currently supporting
        /// </summary>
        IObservableFieldReader<string>SupportingCreatorCode { get;  }
    }
    
    
    /// <inheritidoc cref="IContentCreatorDataProvider" />
    public interface IContentCreatorLogic : IContentCreatorDataProvider
    {
        /// <summary>
        /// Updates content creator this player is currently supporting
        /// </summary>
        void UpdateCreatorSupport(string creatorCode);
    }

    
    /// <inheritdoc cref="IContentCreatorLogic"/>
    public class ContentCreatorLogic : AbstractBaseLogic<ContentCreatorData>, IContentCreatorLogic, IGameLogicInitializer
    {
        private IObservableField<string> _supportingCreatorCode;

        public IObservableFieldReader<string> SupportingCreatorCode => _supportingCreatorCode;
        
        public ContentCreatorLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
        {
        }

        
        public void Init()
        {
            _supportingCreatorCode = new ObservableResolverField<string>(() => Data.SupportingCreatorCode, val => Data.SupportingCreatorCode = val);
        }
        
        public void ReInit()
        {
            {
                var listeners = _supportingCreatorCode.GetObservers();
                _supportingCreatorCode = new ObservableResolverField<string>(() => Data.SupportingCreatorCode, val => Data.SupportingCreatorCode = val);
                _supportingCreatorCode.AddObservers(listeners);
            }
            
            _supportingCreatorCode.InvokeUpdate();
        }
        
        public void UpdateCreatorSupport(string creatorCode)
        {
            if (_supportingCreatorCode.Value.Equals(creatorCode)) {
                return;
            }

            _supportingCreatorCode.Value = creatorCode;
        }

        

        
    }
}