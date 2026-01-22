# CandyBlast - Collapse/Blast Mechanic Game

## 📋 Proje Hakkında

Bu proje, Good Level Up stüdyosu için geliştirilmiş bir **Collapse/Blast mechanic** oyunudur. Toon Blast, Lily's Garden ve Pet Rescue Saga tarzında bir match mekanik sistemi içerir.

### Temel Özellikler

✅ **Dinamik Grid Sistemi** (2-10 satır, 2-10 sütun)  
✅ **Çoklu Renk Desteği** (1-6 renk arası)  
✅ **Dinamik İkon Sistemi** (Grup boyutuna göre değişen sprite'lar)  
✅ **Akıllı Grup Algılama** (Flood-fill algoritması)  
✅ **Fizik ve Gravity** (Bloklar düşer, boşluklar dolar)  
✅ **Deadlock Tespit** (Oynanabilir hamle kontrolü)  
✅ **Smart Shuffle** (Kör shuffle değil, oynanabilir sonuç garantili)  
✅ **Performans Optimizasyonu** (Object pooling, efficient algorithms)  

## 🎮 Oyun Mekaniği

### Temel Kurallar

- **Minimum Grup Boyutu**: 2 blok (aynı renk, bitişik)
- **Blast**: Gruba tıklayarak blokları yok et
- **Gravity**: Yok edilen blokların yerine üstten yenileri düşer
- **Cascade**: Otomatik zincirleme eşleşmeler
- **Deadlock**: Hamle kalmadığında otomatik shuffle

### İkon Sistemi

Bloklar grup boyutuna göre farklı sprite'lar gösterir:

| Grup Boyutu | İkon Tipi | Açıklama |
|-------------|-----------|----------|
| ≤ A | Default | Varsayılan sprite |
| A < size ≤ B | Icon A | İlk varyant |
| B < size ≤ C | Icon B | İkinci varyant |
| > C | Icon C | Üçüncü varyant |

**Örnek**: A=4, B=7, C=9 ise:
- 1-4 blok → Default icon
- 5-7 blok → Icon A
- 8-9 blok → Icon B
- 10+ blok → Icon C

## 🏗️ Proje Yapısı

### Core Scripts

```
Assets/Scripts/
├── BlockType.cs          # Enum tanımlamaları
├── Block.cs              # Tek blok yönetimi + animasyonlar
├── BlockPool.cs          # Object pooling sistemi
├── Board.cs              # Grid yönetimi, blast, gravity
├── GroupDetector.cs      # Grup algılama algoritması
├── LevelManager.cs       # Level konfigürasyonu
├── InputManager.cs       # Oyuncu input yönetimi
├── GameUI.cs             # UI yönetimi
└── SpriteManager.cs      # Sprite yönetimi (ScriptableObject)
```

### Performans Optimizasyonları

#### CPU Optimizasyonu
- ✅ **O(1) Grid Access**: 2D array kullanımı
- ✅ **Efficient Flood Fill**: HashSet ile visited tracking
- ✅ **Event-Driven Architecture**: Gereksiz Update() çağrıları yok
- ✅ **Smart Algorithms**: Kör shuffle yerine garantili sonuç

#### Memory Optimizasyonu
- ✅ **Object Pooling**: Block'lar yeniden kullanılır
- ✅ **Struct-like Data**: Minimal memory footprint
- ✅ **Dictionary Caching**: Sprite lookup O(1)
- ✅ **No Runtime Allocations**: Pre-allocated collections

#### GPU Optimizasyonu
- ✅ **DOTween Animations**: Hardware accelerated
- ✅ **Sprite Atlas Ready**: Batch rendering için hazır
- ✅ **Minimal Draw Calls**: SpriteRenderer batching
- ✅ **Particle Systems**: GPU particle effects

## 🚀 Kurulum

### Gereksinimler

- Unity 2022.3 LTS veya üzeri
- TextMeshPro (Package Manager)
- DOTween (Third-party) ✅ Yüklü
- Odin Inspector (Third-party) ✅ Yüklü

### Setup Adımları

1. **SpriteManager Asset Oluşturma**
   ```
   Assets/Resources klasörü oluşturun
   Textures klasöründeki sprite'ları Resources/Textures içine taşıyın
   Sağ tık → Create → CandyBlast → Sprite Manager
   Inspector'da "Auto-Load Sprites" butonuna tıklayın
   ```

2. **Scene Setup**
   - GameScene açın
   - Boş GameObject oluştur → "GameManager"
   - Components ekle:
     - LevelManager
     - Board
     - BlockPool
     - InputManager
     - GameUI (Canvas içinde)

3. **Prefab Oluşturma**
   ```
   Block prefab:
   - Sprite Renderer ekle
   - Block.cs script ekle
   - (Opsiyonel) Particle System ekle
   - Prefab olarak kaydet
   ```

4. **Board Configuration**
   - BlockPool → Block Prefab referansı
   - Board → BlockPool ve SpriteManager referansları
   - LevelManager → Board referansı

5. **Input Setup**
   - InputManager → Board ve Camera referansları
   - Layer oluştur: "Block"
   - Block prefab'ı Block layer'ına ata

## 🎯 Level Konfigürasyonu

### Örnek 1 (PDF'den)
```csharp
M = 10  // Satır
N = 12  // Sütun
K = 6   // Renk sayısı
A = 4   // İlk eşik
B = 7   // İkinci eşik
C = 9   // Üçüncü eşik
```

### Örnek 2 (PDF'den)
```csharp
M = 5   // Satır
N = 8   // Sütun
K = 4   // Renk sayısı
A = 4   // İlk eşik
B = 6   // İkinci eşik
C = 8   // Üçüncü eşik
```

LevelManager Inspector'ında preset butonlar mevcut!

## 🔍 Algoritma Detayları

### Grup Algılama (Flood Fill)
```
Complexity: O(M*N) worst case
Average: O(group_size)
```

**Mantık**:
1. Tıklanan bloğun rengini al
2. Flood-fill ile bitişik aynı renkli blokları bul
3. HashSet ile ziyaret edilenleri takip et
4. Minimum 2 blok kontrolü

### Smart Shuffle
```
Complexity: O(attempts * M*N)
Max attempts: 100
```

**Mantık**:
1. Tüm blokları topla
2. Fisher-Yates shuffle
3. Yerleştir ve valid grup kontrolü yap
4. Grup yoksa tekrar shuffle
5. Valid grup bulunana kadar devam

### Deadlock Detection
```
Complexity: O(M*N)
```

**Mantık**:
1. Tüm blokları tara
2. Her blok için grup bul (flood-fill)
3. Size ≥ 2 grup varsa → valid
4. Hiç valid grup yoksa → deadlock

## 📊 Performans Metrikleri

### Hedef Değerler
- **FPS**: 60 (mobile)
- **Memory**: < 100MB
- **Loading**: < 2 saniye
- **Input Latency**: < 16ms

### Profiling Noktaları
```csharp
// Board.cs içinde pool stats
Debug.Log(blockPool.GetPoolStats());

// Frame time tracking
Time.deltaTime // Unity Profiler ile izleyin
```

## 🎨 Sprite Naming Convention

Textures klasöründeki dosyalar:
```
{Color}_{Variant}.png

Örnekler:
Blue_Default.png
Blue_A.png
Blue_B.png
Blue_C.png
```

Renkler: Blue, Green, Pink, Purple, Red, Yellow  
Varyantlar: Default, A, B, C

## 🐛 Debugging

### Inspector'da Test Butonları
- **LevelManager**: "Load Example 1/2" - Preset ayarlar
- **Board**: "Initialize Board" - Tahtayı yeniden oluştur
- **Board**: "Shuffle Board" - Manual shuffle

### Debug Logs
```csharp
// Blast events
"Blasted X blocks! Total score: Y"

// Deadlock
"Deadlock detected! No valid moves available."

// Shuffle
"Auto-shuffling board..."
```

## 📱 Mobile Build Settings

### Performance Settings
```
Player Settings:
- Graphics API: Vulkan (Android) / Metal (iOS)
- Multithreaded Rendering: ON
- Static Batching: ON
- Dynamic Batching: ON
- GPU Skinning: ON
```

### Optimization Tips
1. **Sprite Atlas** kullanın (draw call reduction)
2. **Particle Count** limitlyin
3. **Canvas** static olarak işaretleyin
4. **Raycast Target** gereksiz UI'larda kapatın

## 📦 Export

Library klasörünü hariç tutarak zip:
```bash
# PowerShell
Compress-Archive -Path "Assets","Packages","ProjectSettings" -DestinationPath "CandyBlast.zip"
```

## 🎓 Teknik Detaylar

### Design Patterns
- **Singleton**: LevelManager (global access)
- **Object Pool**: BlockPool (memory efficiency)
- **Observer**: Events (loose coupling)
- **Strategy**: GroupDetector (algorithm separation)

### SOLID Principles
- **S**: Her class tek sorumluluğa sahip
- **O**: Yeni block type'ları kolayca eklenebilir
- **L**: Block interface ile genişletilebilir
- **I**: Minimal interface'ler
- **D**: Dependency injection (references)

## 📖 Referanslar

- PDF Case Study: `Assets/Instruction/GJG_Game_Dev_Summer-Internship_Case.pdf`
- DOTween Docs: [dotween.demigiant.com](http://dotween.demigiant.com)
- Odin Inspector: [odininspector.com](https://odininspector.com)

---

**Geliştirici Notu**: Kod içi yorumlar performance ve optimization odaklı yazılmıştır. Her kritik algoritma için complexity notları eklenmiştir.
