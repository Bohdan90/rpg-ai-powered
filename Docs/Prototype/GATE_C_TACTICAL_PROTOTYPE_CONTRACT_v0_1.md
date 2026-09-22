# RPG AI Powered — Gate C: Tactical Prototype Contract v0.1

Дата: 2026-09-21. Статус: **PROTOTYPE SPEC / NOT THEMATIC OWNER**.

Переход в Gate C принят пользователем. Этот документ задаёт Step 0 и implementation plan Step 1. Это не результат playtest и не новая редакция игрового канона. Реализация graybox ещё не выполнена. Google Drive owners остаются authoritative; изменения в Drive не вносились.

## 1. Один вопрос и граница вывода

**Создаёт ли базовый combat loop несколько понятных осмысленных решений при сопоставимых армиях — за счёт позиции, Movement/Action, защиты и физического отхода?**

Проверяем выбор между продвижением и удержанием, прицельным выстрелом и перемещением, frontal engagement и флангом, сохранением юнита и удержанием поля. Не проверяем полноценный баланс рас, магию, кампанию или финальную продолжительность боя.

Риски из предыдущего аудита: R04/R06/R11/R13; частичные T12/T09. Это ещё не полный T06 и не persistence-тест T02/T03.

## 2. Provenance и обозначения

- **F — FIXED:** воспроизведение существующего правила.
- **T — PROTOTYPE TUNING:** временное число для незакрытой числовой части.
- **P — PROTOTYPE ASSUMPTION:** временное исполняемое уточнение неполного правила. Оно не объявляется существующим canon и не переносится в owners автоматически.
- **D — DEFER:** система отсутствует в этом эксперименте; выводов о ней нет.

Проверены свежие 00/07/09 и профильные owners. Основные ссылки:

- [01_MASTER_CANON](https://drive.google.com/file/d/1Igdf9M5kHiLEm5AT3YATIGARHsrkW5eX/view): общий контракт тактики и Accuracy → Guard → routing.
- [02_PURE_CLASSES_AND_MAGIC](https://drive.google.com/file/d/10mWga5-mMGb_YL1lx0Su55Jg09V8DkQF/view): Human Warrior TI, Human Archer, Elf Warrior TI.
- [04_LIVING_ARMOR_AND_SHARED_MECHANICS](https://drive.google.com/file/d/1M1spQ4MI891LLQZKRIEO7xfuV7P8UvgH/view): Movement/Action, Defend, защиты, отсутствие общего backstab bonus.
- [12_STATUS_REGISTRY](https://drive.google.com/file/d/1I2aObdJgJFFaTk_gxRoazhPfzoRoo4X-/view): Defending, Dodge/Guard и Reaction vocabulary.
- [17_COMMANDERS_CAPTIVITY_RETREAT](https://drive.google.com/file/d/1U5acCffJ4y6LL6ZoSBEjVhe8rP1Zd395/view): Commander, Command Load, §9.5 результаты частичного/полного отхода.
- [19_TACTICAL_DEPLOYMENT_RETREAT](https://drive.google.com/file/d/1OR6fJPuDOwuubAOlJ3PWkaGAq9Wjb00O/view): deployment, отдельная задняя Retreat Zone, физическая эвакуация.

## 3. FIXED-подмножество и зачем оно нужно

| Включено — F | Проверяемый выбор |
|---|---|
| Отдельные юниты; Movement и Initiative — разные параметры; Movement + Action | Позиция против темпа атаки |
| Basic Attack расходует Action; нет универсального Delay/Ready | Значение текущей активации |
| Defend только до расходования Movement, расходует Action и оставшееся Movement, до начала следующей собственной активации | Защита против продвижения/урона |
| Accuracy/Dodge → один eligible Guard → Physical damage в Armor/HP | Предсказуемость и ценность защиты |
| Facing без универсального backstab bonus; Elf Frontal Evasion только спереди | Фланг через существующую защиту, без нового бонуса урона |
| ZoC/OA и обычное движение при retreat | Цена выхода из контакта |
| Retreat Zone позади deployment; вышедший юнит Escaped/Safe | Сохранение юнита против присутствия на поле |
| Частичный отход не означает поражение; Commander — физическая фигура, его смерть не удаляет армию | Продолжение боя после потерь |
| Все участвующие юниты присутствуют с начала боя | Нет подмены масштаба очередью подкреплений |

## 4. Конкретные профили и состав

1. **Human Warrior TI:** arming sword + ordinary shield, medium Armor, один Physical melee Basic. Никакого Shield Wall/Defender на TI.
2. **Human Archer TI:** обычный bow, один Physical Basic Shot; Steady Aim при выстреле без предшествующего Movement. Никаких Target Mark/Fire Lane/Crit/AP riders.
3. **Elf Warrior TI:** один elven sword, light Armor, повышенные Movement/Initiative, Frontal Evasion. Никаких dual blades, Graceful Exit, Bleed, Flow Through.

Итого **3 существующих профиля**, все TI/progression forms, не finals.

Зеркальный состав каждой стороны: `HW-Commander`, `HW-Infantry`, `HA-Left`, `HA-Right`, `EW-Flanker`. Итого **5 фигур на сторону**. Commander использует тот же HW TI боевой профиль, с флагом роли; специальные Commander modifiers в этом изолированном тесте отключены и не валидируются.

Fixture: Human Commander Rank I, Capacity 32; подчинённые HW + HA + HA + Unfamiliar EW дают `6 + 6 + 6 + 9 = 27`, что укладывается в существующий предел. Это проверка легальности подготовленного состава, не реализация системы формирования армий. Число фигур — размер сценария, не новая army cap.

Все следующие значения — **T**, не баланс классов:

| Профиль | HP | Armor | Movement | Initiative | Accuracy, % | Dodge, п.п. | Guard, % | Basic damage | Range |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| HW TI | 40 | 16 | 4 | 10 | 85 | 5 | 20 | 12 | 1 |
| HA TI | 28 | 4 | 4 | 12 | 80 | 5 | 0 | 10 | 10 |
| EW TI | 32 | 6 | 6 | 14 | 85 | 10 | 0 | 11 | 1 |

Base Physical Resistance = 0. Ward/Barrier = 0; они не реализуются. Все фигуры 1×1. Нет случайного разброса damage и Crit.

- Steady Aim: **+15 п.п. Accuracy, без увеличения Range (максимум bow всегда 10)**, при отсутствии потраченного Movement перед выстрелом; оставшееся Movement обнуляется. Никакого дополнительного Crit/AP. T чисел, F поведения.
- Frontal Evasion: **+15 п.п. Dodge** против eligible прямой фронтальной атаки. T числа, F ограничения.
- Defending: **25% Physical Resistance** вместо базовых 0. **P** выбора конкретного defensive benefit, **T** величины; не новый pool и не добавочная Guard-проверка.

## 5. Battlefield и deployment — P/T

- Квадратная сетка **13×9**; координаты x=0…12, y=0…8. Восемь направлений; один шаг, в том числе диагональный, стоит 1 Movement; расстояние Chebyshev.
- Все клетки ровные. Solid obstacles непроходимы и блокируют LoS. Высоты и Difficult Terrain — D. Unit Cover уточнён ниже в поправке Milestone 2B.1.
- West Retreat Zone: x=0; East: x=12, вся высота. Deployment: x=1…2 и x=10…11, y=1…7. Они не пересекаются с Retreat Zone.
- Default West: Commander (2,4), Infantry (2,3), HA-Left (1,2), HA-Right (1,6), Flanker (2,5). East — отражение x→12−x. Facing — к противнику.
- До начала можно переставлять своих юнитов внутри deployment. AI commits свою расстановку до player deployment; противник показывается после подтверждения. Никакого General Staff advantage.
- Базовая карта содержит obstacles (6,3), (6,4), (6,5), обходы сверху/снизу. Контрольная карта — те же размеры без obstacles. Третий fixture — та же базовая карта с wounded EW: HP=8, Armor=0; это изолированный тест эвакуации, не сравнение равных армий.
- Обычный Move не проходит через занятые клетки, в том числе союзные. Для диагонали обе прилегающие ортогональные клетки должны быть свободны от препятствий и фигур. Path выбирается и показывается явно; hidden auto-detour через OA нет.
- Ranged LoS: center-to-center segment блокируется при пересечении внутренности solid клетки или точном прохождении через общий угол двух диагонально соприкасающихся solid клеток (sealed zero-width gap). Касание угла одной стены и движение вдоль её границы сами по себе разрешены. Клетки стрелка/цели исключаются. Промежуточные активные юниты дают направленный Cover по прежнему inclusive supercover, но не блокируют LoS. Для diagonal melee достаточно одного открытого orthogonal бока; два solid бока запрещают контакт. Movement сохраняет отдельный более строгий corner contract. См. последние локальные playtest-поправки ниже.

## 6. Активации и facing — P, кроме отмеченных F

Round содержит по одной активации каждой активной фигуры. Очередь: Initiative по убыванию, затем seeded tie key, назначенный фигуре один раз на старте; unit ID — последний tie-break. Начальный seed/ключи входят в лог. Нет командных ходов или дополнительных активаций за высокую Initiative.

На старте своей активации: снять Defending **F**; восстановить Movement и один Action **F**; восстановить OA availability **P**. Удалённые/Dead/Escaped фигуры пропускаются.

**P — временный порядок v0.1:** можно тратить Movement несколькими командами, затем сделать один Basic/Defend и закончить активацию; после расходования Action дальнейшее Movement не допускается. Можно атаковать без движения или закончить активацию раньше. Универсальный порядок move-before-action не найден как полный FIXED контракт: это явное допущение стенда, а не перенос Windrunner/других специальных move-after-attack правил на всех.

Facing: восемь направлений. Успешный шаг ориентирует фигуру вдоль шага; атака — к цели. При завершении своей активации разрешён один выбор конечного facing без отдельного ресурса **P**; End с неиспользованными ресурсами их теряет. В чужую активацию free rotation нет; OA не поворачивает реагирующего. Нет face-target при попадании.

Для фронта: направление на источник округляется к ближайшему из восьми секторов (точная граница — по часовой стрелке); текущий сектор facing и два соседних считаются frontal. Только frontal даёт EW дополнительный Dodge. Side/rear не получают общего бонуса damage.

## 7. Attack и damage — исполняемая версия

1. Проверить actor/Action, цель, Range/LoS и наличие живой фигуры; при invalid команде ресурсы/RNG не меняются.
2. Израсходовать Action. Для Bow Shot HA без потраченного Movement применить Steady Aim; Melee Strike не получает Aim.
3. **P/T:** `contactChance = clamp(Accuracy + Aim − Dodge − FrontalEvasion − DistancePenalty + CoverAccuracyModifier, 5, 95)%`. Для HA DistancePenalty = `5 × max(0, distance−4)` п.п.; для melee = 0. Один seeded roll контакта; отдельного третьего Dodge roll нет.
4. Если контакт прошёл: один eligible Guard roll **F**. В fixture shield HW даёт 20% против frontal прямых атак **P/T**; других источников Guard нет. Guard не расходует OA и может проверяться при каждой подходящей атаке. Успешный Guard прекращает damage; никакой retaliation.
5. **P:** `D = floor(BasicDamage × (1 − PhysicalResistance))`; `armorLoss=min(currentArmor,D)`; `hpLoss=min(currentHP,D−armorLoss)`. Результат не уходит ниже нуля. В v0.1 нет penetration, signed layer modifiers, смешанных компонентов или сложного modified spill.
6. HP=0 → Dead, убрать из активной очереди и освободить клетку **P представления тела**; record unitId и остаточные pools. Corpse/remains gameplay — D.

Все attacks здесь одиночные Physical. Механика Friendly Fire в проекте сохраняется, но AoE/линии и её содержательная проверка — D. Для debug/hotseat можно явно выбрать союзную цель с подтверждением; само промахнувшееся оружие не перенаправляется на соседнюю фигуру. AI союзников не атакует.

Пример без Guard: damage 12 против Defending → D=9; при Armor=4 теряется 4 Armor и 5 HP. UI обязан показывать вероятность контакта, отдельный Guard, ожидаемый damage при попадании и фактический roll/result.

## 8. ZoC / Opportunity Attack — F наличие, P/T детали

- В v0.1 ZoC создают HW/EW с melee Basic: восемь соседних клеток, кроме sealed диагонального угла с двумя solid боковыми клетками. HA не создаёт ZoC/OA; новый Action-only Melee Strike не меняет это правило. ZoC визуализирует угрозу; сам по себе не останавливает движение и не добавляет стоимость **P**.
- OA trigger: добровольный шаг из adjacency конкретного врага в клетку вне его adjacency. Вход и переход между клетками, сохраняющими adjacency, не вызывают OA **P**.
- Один OA на фигуру между началами её собственных активаций; на старте боя один доступен **P/T**. Это отдельная availability, не Action и не общий новый AP pool.
- OA — один обычный melee Basic [Reaction], damage coefficient 1.0 **T**. Проверки Accuracy/Guard/Armor обычные. Нет автоматической ответной атаки на удар.
- OA разрешается **перед выходным шагом**, с facing уходящего до этого шага. Несколько источников: текущий initiative priority, затем ID. Каждый повторно проверяет legal state; после смерти mover остальные не атакуют и не тратят availability. OA не порождает цепочку OA.
- После OA, если mover жив, выполнить шаг и оплатить Movement. Успешный OA сам по себе не root/stop. Затем проверить вход в собственную Retreat Zone.
- UI заранее показывает конкретные реагирующие фигуры и оставшийся OA. Принятие рискованного пути явно подтверждается; подтверждение не меняет правила.

## 9. Retreat и завершение боя

- Войти в свою Retreat Zone обычным Move → немедленно `Escaped/Safe`, убрать с поля, сохранить HP/Armor. Вражеский край не эвакуирует. Нельзя Deploy/Teleport туда, нажать instant army retreat или пройти сквозь фигуры **F/P геометрии**.
- Уход отдельного юнита не заканчивает бой; смерть/уход Commander тоже не означает auto-defeat **F**.
- Если после разрешения текущей команды у одной стороны нет активных фигур, а у другой есть: оставшаяся сторона Victory; ушедшая последней фигурой сторона — Defeat/Withdrawal; потерявшая последнюю фигуру от damage — Defeat/Eliminated. Ранние evacuees остаются Safe **F**.
- Surrender/capture и incapacitated states — D. Случай обеих пустых сторон недостижим обычной последовательной атакой этого fixture; считать ошибкой сценария, не придумывать canonical draw rule.
- Нет игрового round limit, sudden death, countdown или бонуса за искусственную цель. Если тест завис в взаимном Defend/kiting, оператор завершает его как `TEST_ABORT / STALEMATE_OBSERVED`, не как каноническую ничью. Это результат проверки.
- Morale/casualty/Commander-loss penalties в v0.1 — **D / disabled test subsystem**, не нулевые canonical штрафы. Фиксировать события для следующей стадии; не делать выводов о полной жизнеспособности арьергарда.

## 10. Минимальный AI — P, не strategic AI

Тот же список legal commands, тот же resolver; доступ только к видимому состоянию, без будущих RNG rolls. Отдельный UI toggle hotseat для проверки, что «хорошая тактика» не является эксплуатацией слабого AI.

One-ply кандидаты: stay/достижимые endpoint + legal attack либо End; Defend только без Movement. Для каждого endpoint рассматривать least-cost path и минимальный expected-OA-loss path в рамках Movement, чтобы AI не игнорировал безопасный обход.

Первичный policy: при HP≤25% и достижимом собственном крае предпочесть физическую эвакуацию по пути с минимальным expected HP loss. Порог — T; это эвристика, не forced morale retreat. Остальные кандидаты оцениваются:

`S = expected enemy HP loss + 0.5×enemy Armor loss + 12×killProbability − expected OA HP loss − 0.5×expected incoming HP loss + approachBonus`.

Incoming: сумма лучшей одиночной атаки каждого живого врага из его достижимых клеток до нашей следующей активации, без моделирования coordinated combos. ApproachBonus = снижение расстояния до ближайшего врага, ограниченное диапазоном −2…2. Tie-break: меньше потраченного Movement, затем стабильный command order. Все веса — T, не новый gameplay score/Effective Power.

Preview/AI evaluation не двигает combat RNG. Решение AI и оценка пишутся в debug log. Если policy ошибается, проверять ситуацию hotseat до вывода о провале combat loop.

## 11. Telemetry и критерии

Локальный JSONL export; без analytics server. Header: contract/build/config version, scenario/seed, initial state и initial deployment, controller per side. Event: sequence, round, actor/target ID, command/path/facing, legal/invalid reason, Movement/Action/OA before/after, contact/Guard chances и rolls, HP/Armor delta, Defend/Escape/Death, result. RNG state хранится в snapshot. Результат: active/dead/escaped IDs и остатки pools.

Session log отдельно от replay: время выбора активного игрока без background-tab пауз, число отменённых previews и invalid attempts, использованные actions, OA exposures, attacks before first HP loss, rounds/duration. Не трактовать время раздумий само по себе как глубину.

На подготовленных повторяемых ситуациях спросить игрока до действия: «Какие варианты рассматриваешь? Что потеряешь/получишь? Что произойдёт при отходе?» Сопоставить прогноз и результат. Наблюдение ручное; click logs не доказывают осмысленность.

**Технический DoD:** развернуть состав, доиграть каждый fixture до результата/наблюдаемого stalemate, выгрузить лог, воспроизвести тот же результат при том же initial state + seed + command sequence; отсутствие нарушений action/deployment/escape правил.

**Design decision после теста:** продолжать к persistence, если игрок понимает основные последствия и разумный выбор меняется при изменении позиции/состояния. Не требовать ровно двух одинаково сильных ходов каждую активацию. Если один opener побеждает независимо от позиции, Defend лишь затягивает бой или flanking не меняет решения — сначала локальная проверка tuning/геометрии/AI, не новый список abilities. При зависимости результата от P-допущения отдельно пересмотреть именно его.

Нет заранее выдуманной нормы «бой 10 минут» и заявления PASS без наблюдений. Масштаб нескольких армий, магия и ценность стратегических потерь остаются непроверенными.

## 12. DEFER — запрет расширения v0.1

Spells, Ward/Barrier, Living Armor, status catalogue, summons, heals, resurrection, artifacts, secondary weapons, all higher tiers, Commander doctrines/auras/XP/morale, campaign framework, economy, city/research/culture implementation, sieges, worldgen, strategic AI/autoresolve, production art/audio, polished UI и narrative branches.

Причина отсутствия мага: выбранный вопрос проверяется тремя полными ранними профилями без изобретения числового/поведенческого контракта Fire Armor и отдельного spell resource/cooldown layer. Это не отказ от магии в игре. City/Culture data-model experiment — отдельная задача и не dependency сборки.

## 13. Step 1 — минимальная техническая архитектура

**Рекомендация для этого проверочного стенда:** TypeScript + Canvas 2D + простой DOM HUD; Vite vanilla-ts; Vitest для resolver tests. Browser/local build, без backend и логина. Это не выбор финального game engine. [Vite guide](https://vite.dev/guide/), [Vitest guide](https://vitest.dev/guide/).

Один небольшой проект:

| Модуль | Единственная ответственность |
|---|---|
| `config.ts` | versioned tuning, три profile definitions, фиксированные scenarios |
| `battle.ts` | BattleState, legal commands, activation queue, seeded RNG, applyCommand → state + events |
| `geometry.ts` | pathfinding, LoS, adjacency, facing; общие для UI/AI/resolver |
| `ai.ts` | Выбор одной legal команды через тот же preview |
| `view.ts` | Canvas tokens, pools, arrows, highlights; DOM buttons и explanatory panel |
| `main.ts` | Input, startup/reset, session timer, JSON import/export |
| `battle.test.ts` | Проверки значимых инвариантов и replay |

UI не меняет state напрямую. AI не имеет собственного combat resolver. Не нужен event-sourcing framework: достаточно ordinary state, command log и событий для объяснения. Не нужны ECS, plugin API, generalized effect graph, универсальный editor, campaign database или speculative abstraction под все классы.

Placeholders: круги/квадраты с HW/HA/EW, цвет стороны, маленькая Commander-метка, стрелка facing; отдельные HP/Armor bars. Никаких ассетов, требующих генерации.

Минимальные controls: выбрать unit/действие/клетку, подтвердить рискованный путь, Defend, End, deployment confirm, restart same seed, hotseat toggle, export. На экране: activation queue, reachable path, range/LoS, вероятности и damage, ZoC/OA markers, Retreat Zone, одна строка причины запрета.

## 14. Implementation plan и проверки

1. **Data + pure resolver:** profiles/config, state, RNG, очередь, Basic/Defend, event records. Проверить детерминизм и расчёт Armor→HP.
2. **Geometry + board:** deployment, Move/path, LoS/facing, highlights. Получить управляемый бой hotseat.
3. **ZoC/OA + physical retreat:** поэтапное движение, interrupt, Safe/Dead/field-control results. Доиграть базовый и wounded fixtures.
4. **Минимальный AI + explanations:** тот же resolver, preview, queue, probability/result panel. Получить первый playable против AI.
5. **Telemetry + targeted QA:** export/replay, записанный прогон сценариев, затем наблюдение за игроками. Исправлять блокеры проверки; не добавлять abilities ради новизны.

Обязательные проверки: Move→return не возвращает право Defend; Defending заканчивается на собственном start; Steady Aim не даёт Crit/AP; frontal vs side EW различаются только ожидаемым Dodge; Guard не расходует OA; выход из adjacency вызывает OA, движение внутри — нет; OA-kill до шага в Retreat Zone не создаёт Escaped; успешный Escape сохраняет pools; partial retreat не завершает бой; смерть Commander не удаляет союзников; preview не меняет RNG; replay совпадает; AI выдаёт только legal команды; occupied/solid corner нельзя пересечь.

**Следующая стадия после первого рабочего боя:** persistence/несколько последовательных боёв. Сейчас BattleResult уже сохраняет identity, HP/Armor и active/dead/escaped; позже добавить стратегическую оболочку и восстановление по owner 26. Не переключаться после первого боя на десятки новых abilities.

## 15. Что этот контракт не утверждает

Ни один класс не redesign. Выбраны существующие TI kits, без поздних signatures. Ни одно P/T допущение не стало FIXED. Никакой playtest не объявлен пройденным. Drive не обновлялся. Перед production-кодом соответствующего глобального правила прототипное допущение должно быть либо подтверждено, либо заменено owner-решением; для текущего изолированного теста оно имеет явную версию и границы.


## 16. Milestone 2B.1 — playtest corrections (2026-09-21)

Эта локальная поправка заменяет прежнюю полную блокировку ranged LoS промежуточными юнитами.
Light Cover / Strong Cover — существующая лексика проекта; геометрия ниже — **P — PROTOTYPE
ASSUMPTION**, recovered tactical rule pending validation. Изменений тематических Drive owners нет.

- Применяется тот же center-to-center supercover, включая клетки, лишь затронутые углом.
  Solid geometry проверяется по актуальному ranged-контракту §5 и последней поправке; живые активные тела сами по себе не блокируют LoS. Dead/Escaped не дают Cover.
- **P:** близость определяется проекцией центра клетки screener вдоль направления выстрела:
  `2 * dot(screen - shooter, target - shooter) >= squaredLength(target - shooter)`.
  Равенство относится к цели. Это эквивалентно сравнению квадратов евклидовых расстояний
  до концов, а не Chebyshev distance для Range. Обратный выстрел может менять Cover.
- Только пересечённые supercover тела на целевой половине дают Cover, независимо от стороны.
  Shooter/target исключены. Screening size <= target size → Light; больше → Strong.
  Все текущие профили имеют одинаковый размер (rank 1) и занимают 1×1; новые существа не добавляются.
  Сравнение иных размеров доступно в чистой классификации; их battlefield execution остаётся D.
- Cover не суммируется: выбирается None < Light < Strong.
- **T — PROTOTYPE TUNING:** Light Cover = −15 процентных пунктов Accuracy, до clamp 5…95.
  Для Strong чисел нет; query численного модификатора возвращает отсутствие значения.
  Нельзя считать отсутствие значения нулевой защитой; Strong combat behavior остаётся D.
- Cover применяется только к ranged контакту. Урон/Guard/retaliation/Friendly Fire не меняются;
  промах не перенаправляется в screener. Preview не расходует RNG и показывает все слагаемые.
- **P — default path selection:** сначала минимальная стоимость, затем минимум суммы абсолютных cross products
  отклонения от прямой start→destination, затем минимум поворотов,
  затем лексикографический порядок направлений N, NE, E, SE, S, SW, W, NW.
  Первый шаг не считается поворотом относительно исходного facing. BFS даёт минимальную длину;
  DP по shortest-path edges хранит лучший prefix для каждой клетки/входящего направления.
  Выбранный explicit path показывается до подтверждения. Стоимость шагов, occupancy и corners
  не меняются; все ранее легальные explicit paths остаются легальными.


## Local playtest correction — open melee corner / field V2 (2026-09-21)

Explicit user-approved prototype contact rule, superseding the older ambiguous “solid corner” wording for melee only:
- Diagonal-adjacent melee contact is legal with zero or one solid orthogonal side cell; two solid side cells seal the corner and prohibit contact.
- Basic melee attack validation/preview and ZoC/OA adjacency use the same Core helper. Occupying units in side cells remain irrelevant to melee corner geometry; no ranged unit screening is added to melee.
- Movement/pathfinding still prohibit a diagonal step if either orthogonal side cell is solid or occupied. At this earlier melee-only checkpoint ranged supercover still blocked every touched solid corner; that historical behavior is superseded by the later ranged correction below.
- `Field_19x13_ExpandedV2` is an additional comparison fixture, not a final size: obstacles `(9,5),(9,6),(9,7)`; West retreat `x=0`, East `x=18`. Same armies/tuning/seed/rules except this explicitly authorized contact correction, which applies consistently to every fixture.
- User playtest finding: 13×9 and 23×17 too small; 17×11 still somewhat small; 27×21 likely a lower bound with future exterior siege geometry. Larger siege comparisons remain deferred; no moat/bridge/gate mechanics implemented.

See `FIELD_V2_CORNER_CONTACT.md` for reproduction, exact deployment, tests and integration validation. This local implementation amendment records the user's explicit instruction; it does not claim an edit to the authoritative Drive pack or a final battlefield-size decision.


## Local ranged LoS correction — exposed corners and sealed vertices (2026-09-21)

Latest explicit user clarification supersedes the earlier universal solid-supercover blocker rule:
- A ranged segment entering the open interior of a solid cell is blocked.
- Touching only a single solid corner or travelling along its boundary does not block the shot.
- Exception explicitly requested by the user: passing exactly through the shared vertex of two diagonally opposed solid cells is blocked, even with no interior intersection. A zero-width gap is sealed.
- Basic ranged validation, attack preview and the HUD LoS status use this same Core query. Unit Cover continues to use inclusive supercover and existing directional/size/non-stacking tuning; no numeric wall cover is introduced.
- Melee remains allowed with one blocked orthogonal side, prohibited with two. Movement/pathfinding remain stricter and reject a diagonal step when either side is solid/occupied.
- The earlier answer permitting all two-wall pure touches was explicitly retracted by the user; the sealed-vertex exception above is the final instruction.

See `RANGED_CORNER_LOS_CORRECTION.md` for reproduction, algorithm, tests and manual validation. No battlefield dimensions or unit tuning are finalized by this change.

## Local bow envelope tuning — 2026-09-21

**T — PROTOTYPE TUNING**, not final global canon: Human Archer TI Bow Range is always 10,
with or without Steady Aim. Steady Aim retains +15 pp Accuracy when no Movement has been
spent before the shot and still commits remaining Movement, but no longer adds range.
Distance penalty stays `5 × max(0, distance − 4)` pp: distances 4/5/7/8/9/10 give
0/−5/−15/−20/−25/−30 pp. It stacks normally with Dodge, Frontal Evasion and unit Cover.
Working distance around 7 (1.75 × baseline Movement 4) and maximum 10 (2.5 × baseline)
are experimental reference points only. No minimum range or adjacent-shot penalty is added.
Field_19x13_ExpandedV2 and all other combat/fixture tuning remain unchanged.

## Local HA engagement fallback — P/T, 2026-09-21

User-approved Gate C experiment, not final class canon or weapon identity. Any hostile
active HW/EW whose existing Core ZoC reaches HA locks Bow Shot (Engaged), regardless
of whether that source has already spent OA. Adjacent HA, allies, Dead/Escaped bodies
and sealed corners do not independently lock the bow.
While engaged, HA may use Melee Strike: Range 1, Physical damage 5, Accuracy 80,
normal Action/Guard/Resistance/Armor→HP and facing rules. No Steady Aim, ranged
Cover/distance modifiers, special effects or penetration. It grants no ZoC/OA.
The fallback is available only while engaged; targeting and friendly-fire confirmation
otherwise retain existing Basic rules. Movement out uses ordinary OA; a surviving HA
outside every hostile ZoC regains Bow Shot, without Aim after spending Movement.
See [ARCHER_ENGAGEMENT_FALLBACK.md](ARCHER_ENGAGEMENT_FALLBACK.md).
