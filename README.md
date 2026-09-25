# Mesleki Eğitimin Dijital İkizi: Sanal Atölye

Unity 6.6 ve URP üzerinde çalışan, elektrik panosu montajını öğreten oynanabilir prototip.

## Çalıştırma

1. Projeyi Unity Hub üzerinden aç.
2. `Assets/Scenes/SampleScene` sahnesini aç ve Play'e bas.
3. `WASD` ile hareket et, fareyle bak. Bir parçaya veya pano yuvasına bakıp `E` ile seç/yerleştir. Seçili parçayı `R` ile döndür.
4. Üç parçayı doğru yerlere monte ettikten sonra panodaki renkli terminallere sırayla bakıp `E` ile bağlantı kur.
5. `ESC` fare imlecini serbest bırakır; tekrar tıklamak fareyi kilitler.

Atölye, oyuncu, pano, eğitim adımları ve arayüz Play sırasında `VirtualWorkshop.cs` tarafından oluşturulur. Bu ilk prototipte görseller basit 3B şekillerden üretilir; dışarıdan starter asset gerekmez.

## Prototipte bulunanlar

- FPS hareketi, fareyle kamera kontrolü, zıplama ve koşma.
- Otomatik sigorta, kaçak akım rölesi ve klemens için montaj yuvaları.
- Nesne seçme, taşıma, yerleştirme ve döndürme.
- Doğru/yanlış montaj ile doğru/yanlış terminal bağlantısı kontrolü ve hata kaydı.
- Eleman seçiliyken teknik bilgi gösterimi.
- Puan, süre, hata sayısı ve uygulama sonu performans özeti.
- Temel İSG ve 5S bilgilendirmeleri.

Bu sürüm eğitim senaryosu prototipidir. Gerçek atölye ölçülerine göre modellenmiş varlıklar, senaryo verilerinin öğretmen tarafından düzenlenmesi, kalıcı öğrenci raporları, gerçek ön test/son test uygulaması ve araştırma sonuçları henüz eklenmemiştir. Raporlama, yalnızca uygulama içinde gerçekleşen prototip puanı/süre/hata verisini gösterir; eğitimsel etki kanıtı olarak yorumlanmamalıdır.
