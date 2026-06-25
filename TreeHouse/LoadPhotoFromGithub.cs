
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace Shift.PhotoFrame
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LoadPhotoFromGithub : UdonSharpBehaviour
    {
        [Header("ディスプレイ表示")]
        [Header("表示時間 Display Time[s]")]
        [SerializeField] private float displayTime = 8;
        [Header("初期表示時のみ Offset Time[s] だけ延長して表示する")]
        [SerializeField] private float offsetTime = 0;
        [Header("画像の表示順番をランダム化するかどうか")]
        [SerializeField] private bool isRandom = true;
        [Space(10)]

        [Header("ローカル化")]
        [SerializeField, TextArea(2, 3)] private string msg1 =
            "Is Local OFF：外部から画像を読み込みます。初期表示のみlocalPhotosの要素0を使います。\n" +
            "Is Local ON ：外部リソースを使わずに、localPhotosのテクスチャ配列を使います。";
        [SerializeField, UdonSynced, FieldChangeCallback(nameof(SyncedIsLocal))] private bool isLocal = false;
        [Space(10)]

        [Header("ローカルテクスチャ")]
        [SerializeField, TextArea(1, 2)] private string msg2 =
            "IsLocalがオフだとしても、初期表示として1枚以上のローカルテクスチャ設定が必要です。";
        [SerializeField] private Texture2D[] localPhotos;

        // 初期化済判定フラグ
        private bool isInitialized = false;

        // 同オブジェクトに紐づくメッシュのマテリアル
        private Material mat;
        // 同オブジェクトに紐づくアニメーター
        private Animator animator;

        // 有効なファイル数が記載されている外部テキストへの静的URL
        private VRCUrl fileNumUrl = new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/file_num.txt");
        // 有効なファイル数
        private int validFileCount = 0;

        // 画像（外部参照）への静的URL
        // VRCUrlは基本的に静的URLにしないといけないため、あらかじめ十分な数のURLを宣言しておく
        // URL先に画像の実体があるかどうかは保証されない
        // 上記fileNumUrlに記載されている"有効なファイル数"までは実体があることを想定する
        private const int MaxPhotoCount = 50;
        private VRCUrl[] imageUrls = {
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/00.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/01.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/02.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/03.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/04.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/05.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/06.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/07.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/08.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/09.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/10.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/11.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/12.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/13.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/14.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/15.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/16.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/17.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/18.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/19.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/20.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/21.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/22.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/23.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/24.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/25.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/26.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/27.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/28.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/29.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/30.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/31.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/32.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/33.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/34.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/35.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/36.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/37.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/38.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/39.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/40.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/41.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/42.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/43.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/44.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/45.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/46.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/47.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/48.png"),
            new VRCUrl("https://shift4869.github.io/vrc-shift.github.io/TreeHouse/image/49.png"),
        };
        // 遷移時にDL対象とする画像インデックス
        private int targetPhotoIndex = -1;
        // 現在DL中かのフラグ
        private bool isDownloading = false;
        // 複数DL要求が来た場合の待機中DL対象インデックス
        private int pendingPhotoIndex = -1;
        // 画像の外部参照用ハンドラ
        private VRCImageDownloader imageDownloader;
        // 外部参照用ハンドラのイベントレシーバー
        private IUdonEventReceiver udonEventReceiver;
        // DL済テクスチャ格納用配列
        private Texture2D[] downloadedTextures;

        // 表示画像の画像インデックス
        [UdonSynced, FieldChangeCallback(nameof(SyncedPhotoIndex))] private int photoIndex = 0;
        // 表示画像の一つ前の画像インデックス
        private int previousPhotoIndex = 0;
        // 画像インデックス配列
        private int[] photoOrderList = new int[MaxPhotoCount];
        // 現在の画像インデックス配列の何番目かを表すインデックス
        private int currentOrderIndex = 0;

        private void Start()
        {
            // 開始処理
            // 基本的に最初に呼ばれるが LateJoiner は FieldChangeCallback が先に呼ばれる可能性がある
            Debug.Log("[LoadPhotoFromGithub]Start");
            Initialize();
        }

        private void Initialize()
        {
            // 初期化処理
            Debug.Log("[LoadPhotoFromGithub]Initialize");

            // 初期化済なら2回初期化は行わない
            if(isInitialized)
            {
                return;
            }

            // 初期化済フラグオン
            isInitialized = true;

            // ローカル動作でない場合であっても、1枚以上は初期表示としてローカル画像の設定が必要
            if (localPhotos.Length > 0)
            {
                // コンポーネント変数初期化
                mat = GetComponent<Renderer>().material;
                mat.SetTexture("_MainTex", localPhotos[previousPhotoIndex]);
                mat.SetTexture("_SubTex", localPhotos[photoIndex]);

                animator = GetComponent<Animator>();

                bool useLocalMode = SyncedIsLocal;
                if (useLocalMode && localPhotos.Length > 1)
                {
                    // ローカルで動作、かつ複数枚画像が設定されている場合
                    InitializeLocalMode();
                }
                else if (!useLocalMode)
                {
                    // ローカル動作でない場合
                    // 外部リソースを利用
                    InitializeRemoteMode();
                }
                // ローカルで画像が1枚のみ設定されている場合は何もしない（固定表示）
            }
        }

        public void InitializeLocalMode(){
            // ローカル動作時の初期化処理
            Debug.Log("[LoadPhotoFromGithub]InitializeLocalMode");

            // 呼び出し元の初期化を一度も通らずに呼び出された場合は不正
            if(!isInitialized)
            {
                return;
            }

            // localPhotosの個数がそのまま有効な写真の数になる
            validFileCount = localPhotos.Length;
            if(validFileCount <= 0)
            {
                return;
            }

            // localPhotosをそのまま使うので、すべてDL済としてdownloadedTexturesに格納
            downloadedTextures = new Texture2D[validFileCount];
            for (int i = 0; i < validFileCount; i++)
            {
                downloadedTextures[i] = localPhotos[i];
            }

            if (Networking.IsOwner(this.gameObject))
            {
                // オーナーならば有効ファイル数を上限として画像インデックス配列を生成
                GeneratePhotoOrder(validFileCount);
            }

            // ローカルのループ呼び出しをセット
            SendCustomEventDelayedSeconds(nameof(LocalUpdate), displayTime + offsetTime);
        }

        public void InitializeRemoteMode(){
            // 外部リソース使用時の初期化処理
            Debug.Log("[LoadPhotoFromGithub]InitializeRemoteMode");

            // 呼び出し元の初期化を一度も通らずに呼び出された場合は不正
            if(!isInitialized)
            {
                return;
            }

            // 外部リソース取得用のコンポーネント変数初期化
            imageDownloader = new VRCImageDownloader();
            udonEventReceiver = (IUdonEventReceiver)this;

            // ファイルから有効な画像の数を取得する
            // 外部通信に成功した場合OnStringLoadSuccessが呼ばれ、失敗した場合OnStringLoadErrorが呼ばれる
            VRCStringDownloader.LoadUrl(fileNumUrl, udonEventReceiver);
        }

        private void GeneratePhotoOrder(int maxValidIndex){
            // 画像インデックスのリストを作成する
            // ランダム化のフラグがオンの場合はシャッフルする
            // 結果はphotoOrderListに格納される
            // 基本的にオーナーのみが保持する

            if (maxValidIndex > 0){
                // 有効な最大indexまでの配列を作成する
                int[] tmpIndexList = new int[maxValidIndex];
                for (int i = 0; i < maxValidIndex; i++)
                {
                    tmpIndexList[i] = i;
                }

                // ランダム化
                if (isRandom)
                {
                    VRC.SDKBase.Utilities.ShuffleArray(tmpIndexList);
                }

                // 画像インデックスリスト初期化
                for (int i = 0; i < photoOrderList.Length; i++)
                {
                    photoOrderList[i] = 0;
                }

                // 有効な最大indexまでの値を格納
                for (int i = 0; i < maxValidIndex; i++)
                {
                    photoOrderList[i] = tmpIndexList[i];
                }

                // 採用順序をログに表示
                // photoOrderList = [2, 10, 0, 6, ... < maxValidIndex]
                string str_array = "";
                for (int i = 0; i < maxValidIndex; i++)
                {
                    str_array += photoOrderList[i].ToString() + " ";
                }
                Debug.Log("[LoadPhotoFromGithub]photoOrderList: " + str_array);
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            // 外部リソース使用時の初期化続き

            // DL結果から有効ファイル数取得を試みる
            if (int.TryParse(result.Result, out var count))
            {
                validFileCount = count;
                Debug.Log($"[LoadPhotoFromGithub]validFileCount is {validFileCount}");
            }
            else
            {
                // 有効ファイル数を取得できなかった
                // ローカル画像を使う形式に変更
                Debug.Log($"[LoadPhotoFromGithub]validFileCount getting failed, use local photo mode.");
                SyncedIsLocal = true;
                return;
            }

            // 有効ファイル数を上限としてDL済テクスチャ格納の配列を用意する
            downloadedTextures = new Texture2D[validFileCount];

            if (Networking.IsOwner(this.gameObject))
            {
                // オーナーならば有効ファイル数を上限として画像インデックスリストを生成
                GeneratePhotoOrder(validFileCount);
            }
            else
            {
                // オーナーでない場合、同期された SyncedPhotoIndex を再評価
                SyncedPhotoIndex = photoIndex;
            }

            // ループ呼び出しをセット
            SendCustomEventDelayedSeconds(nameof(ScheduleNextPhoto), displayTime + offsetTime);
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            // 有効ファイル数を取得できなかった
            Debug.LogError($"[LoadPhotoFromGithub]Could not load string {result.Error}, use local photo mode.");
            // ローカル画像を使う形式に変更
            SyncedIsLocal = true;
        }

        public void ScheduleNextPhoto()
        {
            // 外部リソース使用時のループ処理起点
            // Debug.Log("[LoadPhotoFromGithub]ScheduleNextPhoto");
            LoadNext();
            if (Networking.IsOwner(this.gameObject)){
                SendCustomEventDelayedSeconds(nameof(ScheduleNextPhoto), displayTime);
            }
        }

        private void LoadNext()
        {
            // 外部リソース使用時のループ処理
            // Debug.Log("[LoadPhotoFromGithub]LoadNext");

            // 遷移時にDL対象とする画像インデックスを targetPhotoIndex に設定する
            if (Networking.IsOwner(this.gameObject))
            {
                // オーナーなら画像インデックス配列を使用する
                if(validFileCount <= 0)
                {
                    return;
                }
                if (currentOrderIndex == validFileCount - 1)
                    currentOrderIndex = 0;
                else
                    currentOrderIndex += 1;
                Debug.Log($"[LoadPhotoFromGithub]currentOrderIndex is {currentOrderIndex}/{validFileCount}");
                targetPhotoIndex = photoOrderList[currentOrderIndex];
            }
            else{
                // オーナーでないなら同期されているSyncedPhotoIndexを使用する
                targetPhotoIndex = SyncedPhotoIndex;
            }

            // テクスチャ格納用配列の準備ができているか確認
            if(downloadedTextures == null || targetPhotoIndex < 0 || targetPhotoIndex >= downloadedTextures.Length)
            {
                return;
            }

            // targetPhotoIndex 番目のテクスチャが既にDLされてるか確認する
            var nextTexture = downloadedTextures[targetPhotoIndex];
            if (nextTexture != null)
            {
                // targetPhotoIndex 番目のテクスチャを既にDL済の場合
                Debug.Log($"[LoadPhotoFromGithub]texture of {targetPhotoIndex} is already downloaded!");

                // targetPhotoIndex を現在の画像インデックスとして採用して評価
                SyncedPhotoIndex = targetPhotoIndex;
                if (Networking.IsOwner(gameObject))
                {
                    // オーナーなら SyncedPhotoIndex を他のクライアントに同期する
                    RequestSerialization();
                }
            }
            else
            {
                // targetPhotoIndex 番目のテクスチャをまだDLしていない場合
                Debug.Log($"[LoadPhotoFromGithub]texture of {targetPhotoIndex} is not downloaded, thus try new download!");
                // 外部からDLを試みる
                DownloadPhoto(targetPhotoIndex);
            }
        }

        public void DownloadPhoto(int index){
            // 外部からテクスチャのDLを試みる

            // 現在DLが進行中ならペンディングの機構を起動する
            if (isDownloading)
            {
                Debug.Log($"[LoadPhotoFromGithub]Skip Download. Already Other downloading.");
                pendingPhotoIndex = index;
                return;
            }

            // 現在DLが進行中としてフラグオン
            isDownloading = true;

            Debug.Log($"[LoadPhotoFromGithub]Trying Download texture of {index} ...");

            // DL対象とする画像インデックスがそのまま引数 index として渡される
            // または LateJoiner が同期した photoIndex を index として渡してくる
            // いずれにせよL対象とする画像インデックスに再設定する
            targetPhotoIndex = index;

            // 外部リソース取得用のハンドラが準備できているか確認
            if(imageDownloader == null)
            {
                imageDownloader = new VRCImageDownloader();
            }
            if(udonEventReceiver == null)
            {
                udonEventReceiver = (IUdonEventReceiver)this;
            }
            // rgbInfo は一時変数で良い
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;

            // 外部通信に成功した場合 OnImageLoadSuccess が呼ばれ、失敗した場合 OnImageLoadError が呼ばれる
            imageDownloader.DownloadImage(imageUrls[targetPhotoIndex], null, udonEventReceiver, rgbInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            // 画像DL成功
            Debug.Log($"[LoadPhotoFromGithub]Image loaded: {result.SizeInMemoryBytes} bytes.");

            // 現在DLが完了したとしてフラグオフ
            isDownloading = false;

            // キャッシュ
            downloadedTextures[targetPhotoIndex] = result.Result;

            // 画像インデックスを遷移
            SyncedPhotoIndex = targetPhotoIndex;

            if (Networking.IsOwner(gameObject))
            {
                // オーナーなら SyncedPhotoIndex を他のクライアントに同期する
                RequestSerialization();
            }

            // ペンディングされているDLがあるならば再帰
            if (pendingPhotoIndex >= 0)
            {
                int next = pendingPhotoIndex;
                pendingPhotoIndex = -1;
                DownloadPhoto(next);
            }
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            // 画像DL失敗
            Debug.Log($"[LoadPhotoFromGithub]Image not loaded: {result.Error.ToString()}: {result.ErrorMessage}.");

            // 失敗したが現在DLは完了したとしてフラグオフ
            isDownloading = false;

            // ハンドラの後始末
            imageDownloader.Dispose();
            imageDownloader = null;

            // ペンディングされているDLがあるならば再帰
            if (pendingPhotoIndex >= 0)
            {
                int next = pendingPhotoIndex;
                pendingPhotoIndex = -1;
                DownloadPhoto(next);
            }
        }

        public void LocalUpdate()
        {
            // ローカル動作時の表示ループ
            bool useLocalMode = SyncedIsLocal;
            if (useLocalMode && Networking.IsOwner(this.gameObject))
            {
                // オーナーなら画像インデックス配列を使用する
                if (currentOrderIndex == validFileCount - 1)
                    currentOrderIndex = 0;
                else
                    currentOrderIndex += 1;
                SyncedPhotoIndex = photoOrderList[currentOrderIndex];

                // SyncedPhotoIndex を他のクライアントに同期する
                RequestSerialization();
                SendCustomEventDelayedSeconds(nameof(LocalUpdate), displayTime);
            }
        }

        public void ChangePhoto()
        {
            // 現在の photoIndex を元に画像を遷移させる

            // 初期化を一度も通らずに呼び出された場合は不正
            if(!isInitialized)
            {
                return;
            }

            // コンポーネント確認
            if (mat == null || animator == null)
            {
                return;
            }

            // テクスチャ設定
            if (downloadedTextures[previousPhotoIndex] != null)
            {
                mat.SetTexture("_MainTex", downloadedTextures[previousPhotoIndex]);
            }
            else
            {
                mat.SetTexture("_MainTex", null);
            }
            if (downloadedTextures[photoIndex] != null)
            {
                mat.SetTexture("_SubTex", downloadedTextures[photoIndex]);
            }
            else
            {
                mat.SetTexture("_SubTex", null);
            }

            // 遷移開始
            animator.SetBool("Transition", true);
        }

        public void EndTransition()
        {
            // 遷移終了
            animator.SetBool("Transition", false);
        }

        public int SyncedPhotoIndex
        {
            set
            {
                // 画像インデックスが更新されたとき呼び出される
                // LateJoiner は Start 前にここにくることがある
                // Debug.Log("[LoadPhotoFromGithub]SyncedPhotoIndex set");

                // 遷移元と遷移先の画像インデックスを設定
                previousPhotoIndex = photoIndex;
                photoIndex = value;

                // テクスチャ格納用配列の準備ができているか確認
                if(downloadedTextures == null || photoIndex < 0 || photoIndex >= downloadedTextures.Length)
                {
                    // Initialize();
                    return;
                }

                // 遷移先のテクスチャがDLできているか確認
                if(downloadedTextures[photoIndex] == null)
                {
                    // 遷移先のテクスチャがDLできていない場合DLする
                    DownloadPhoto(photoIndex);
                    return;
                }

                // 画像を遷移させる
                SendCustomEvent(nameof(ChangePhoto));
            }
            get => photoIndex;
        }

        public bool SyncedIsLocal
        {
            set
            {
                bool prevState = isLocal;
                if ((!prevState) && value)
                {
                    // isLocal設定がfalseからtrueに変わったときのみローカル動作に変更する
                    Debug.Log("[LoadPhotoFromGithub]SyncedIsLocal set use local photo mode.");
                    isLocal = value;
                    SendCustomEvent(nameof(InitializeLocalMode));
                }
            }
            get => isLocal;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            // オーナーがインスタンスを離れたとき
            // インスタンス内の誰かしらが新たなオーナーになる
            if (Networking.IsOwner(this.gameObject))
            {
                // オーナーは画像インデックスリストを持つ必要があるので生成
                GeneratePhotoOrder(validFileCount);

                // ループもセットしておく
                if(isLocal)
                {
                    SendCustomEventDelayedSeconds(nameof(LocalUpdate), displayTime);
                }
                else
                {
                    SendCustomEventDelayedSeconds(nameof(ScheduleNextPhoto), displayTime);
                }
            }
        }

        private void OnDestroy()
        {
            // ハンドラの後始末
            if (imageDownloader != null)
            {
                imageDownloader.Dispose();
                imageDownloader = null;
            }
        }
    }
}