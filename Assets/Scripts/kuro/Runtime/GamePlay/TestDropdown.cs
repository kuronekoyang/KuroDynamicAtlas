using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace kuro
{
    public class TestDropdown : MonoBehaviour
    {
        public Dropdown Dropdown;
        public DynamicSpriteHookBase DynamicSprite;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ??= new();

        public void Awake()
        {
            AwakeAsync(CancellationTokenSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        private async ValueTask AwakeAsync(CancellationToken cancellationToken)
        {
            var suc = await AtlasManager.WaitInstanceAsync(10, cancellationToken);
            if (cancellationToken.IsCancellationRequested || !suc)
                return;

            if (!Dropdown || !DynamicSprite)
                return;

            var options = AtlasManager.Instance.AtlasDb.DynamicAtlasList
                .Select(x => x.SpriteData.Id.Name)
                .ToList();
            Dropdown.ClearOptions();
            Dropdown.AddOptions(options);
            Dropdown.value = options.IndexOf(DynamicSprite.SpriteId.Name);
            Dropdown.onValueChanged.AddListener(x =>
            {
                if (x >= 0 && x < options.Count)
                {
                    DynamicSprite.SpriteId = new SpriteId(options[x]);
                }
            });
        }
    }
}