# Система заклинаний — Инструкция по настройке

---

## 1. Скрипты — что куда

| Файл | Куда вешать |
|------|-------------|
| `SpellData.cs` | ScriptableObject (не MonoBehaviour) |
| `SpellSystem.cs` | GameObject игрока (или отдельный пустой GO) |
| `SpellProjectile.cs` | Prefab снаряда |
| `RotatingBladeOrbit.cs` | Создаётся автоматически SpellSystem |
| `OrbitalSatellite.cs` | Создаётся автоматически SpellSystem |
| `EnemyMovement.cs` | Заменяет старый EnemyMovement |
| `EnemyStatusEffects.cs` | Добавить на префаб врага |
| `PlayerMovement.cs` | Заменяет старый PlayerMovement |
| `Player.cs` | Заменяет старый Player |
| `LineController.cs` | Заменяет старый LineController |
| `EnemyBlobDissolveSprite.shader` | Заменяет старый шейдер (тот же файл) |

---

## 2. Создание SpellData ассетов

Для КАЖДОГО заклинания:

```
ПКМ в Project → Create → Spells → SpellData
```

Настрой поля:

| Поле | Что указать |
|------|-------------|
| **Gesture Name** | Точное имя жеста из PDollar (например `circle`, `triangle`) |
| **Spell Type** | Выбери из списка |
| **Spell Color** | Цвет линии и эффектов |
| **Cooldown** | 0 = без перезарядки |
| **Is Directional** | ✅ если заклинание смотрит в сторону движения |
| **Show Magic Circle** | ✅ для торжественных заклинаний (Heal, CircularBlast и т.д.) |
| **Cast Sound** | AudioClip звука каста |
| **Damage / Radius** | Основные параметры урона |

---

## 3. Настройка SpellSystem на сцене

На GameObject со SpellSystem:

- **Registered Spells** → перетащи все созданные SpellData ассеты
- **Enemy Layer** → слой врагов (Enemy)
- **Cooldown Text** → TextMeshPro-объект в Canvas (для уведомления о перезарядке)
- **Flash Overlay** → Image в Canvas, цвет белый, Alpha = 0, Raycast Target = OFF  
  *(если нет — просто оставь пустым, флеш тихо пропустится)*
- **Projectile Prefab** → префаб снаряда (см. раздел 4)
- **Magic Circle Radius / Duration** → радиус и длительность магического круга

---

## 4. Prefab снаряда (для Directional / Random / Spiral)

Создай новый GameObject → назови `SpellProjectile`:

```
SpellProjectile (GameObject)
├── SpriteRenderer       ← любой маленький спрайт-кружок/звезда
├── Rigidbody2D          ← Gravity Scale = 0, Collision Detection = Continuous
├── CircleCollider2D     ← Is Trigger = ON, радиус ~0.15
├── TrailRenderer        ← необязательно, для красивого хвоста
└── SpellProjectile.cs   ← компонент
```

Сохрани как Prefab и назначь в поле **Projectile Prefab** у SpellSystem.

---

## 5. Рекомендуемые настройки заклинаний

| Тип | Directional | Magic Circle | Cooldown |
|-----|-------------|--------------|----------|
| LineStrike | ❌ | ❌ | 0 |
| Explosion | ❌ | ❌ | 2 |
| ForwardWave | ✅ | ❌ | 3 |
| CircularBlast | ❌ | ✅ | 5 |
| ChainLightning | ❌ | ❌ | 4 |
| IceExplosion | ❌ | ❌ | 5 |
| FireField | ❌ | ❌ | 8 |
| PushAndSlow | ❌ | ✅ | 4 |
| FreezeForward | ✅ | ❌ | 4 |
| Ignite | ❌ | ❌ | 3 |
| Slow | ❌ | ❌ | 3 |
| RotatingBlades | ❌ | ✅ | 10 |
| OrbitalSatellites | ❌ | ✅ | 12 |
| Laser | ✅ | ❌ | 8 |
| DirectionalProjectiles | ✅ | ❌ | 3 |
| RandomProjectiles | ❌ | ❌ | 2 |
| SpiralProjectiles | ❌ | ✅ | 4 |
| Teleport | ❌ | ❌ | 6 |
| Heal | ❌ | ✅ | 15 |

---

## 6. Prefab врага — что добавить

На каждый префаб врага добавь:
- `EnemyStatusEffects` (автоматически требует `Enemy` и `SpriteRenderer`)

Шейдер `EnemyBlobDissolveSprite` замени на обновлённый — он обратно совместим.

---

## 7. UI для кулдауна

Создай в Canvas:
```
Canvas
└── CooldownText (TextMeshPro)
     ├── Anchor: верх по центру или низ
     ├── Font Size: ~28
     └── Color: желтоватый
```
Назначь в поле **Cooldown Text** у SpellSystem.  
Изначально объект можно скрыть — SpellSystem сам включает/выключает.

---

## 8. Screen Flash Overlay

```
Canvas
└── FlashOverlay (UI Image)
     ├── Anchor: stretch (весь экран)
     ├── Color: белый, Alpha = 0
     ├── Raycast Target = OFF
     └── GameObject изначально выключен
```

---

## 9. Добавление нового заклинания (чеклист)

1. Добавь значение в `enum SpellType` в `SpellData.cs`
2. Добавь `case` в `ExecuteSpell()` в `SpellSystem.cs`
3. Напиши метод заклинания
4. Создай SpellData ассет через ПКМ
5. Добавь ассет в список Registered Spells у SpellSystem
6. Готово

---

## 10. Что было добавлено сверх задания

- **Screen Flash** — мгновенная вспышка экрана на мощных заклинаниях
- **Двойной магический круг** — внутренний круг медленно вращается при касте
- **Индикатор направления** — стрелка-трезубец показывает куда полетит заклинание
- **Затухающие круги зон** — визуализация радиуса заклинания после каста
- **Knockback** через `EnemyMovement.TriggerKnockback()` — физически плавное отбрасывание (без телепортации)
- **Конус для FreezeForward** — анимированный визуал конуса заморозки
- **Анимированная молния** — ломаная линия которая трясётся пока не затухнет
- **Линия рисуется белым** → при успешном касте окрашивается в `spellColor` заклинания
- **SpellSystem не ломает PlayerAttack** — если жест не найден в SpellSystem, он передаётся в оригинальный PlayerAttack
