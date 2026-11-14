<div align="center">

<img src="Images/SakuraTrap-Icon.png" width="140" alt="SakuraTrap" />

# SakuraTrap

**Кастомный лаунчер Roblox с профилями производительности в один клик**

[![CI](https://img.shields.io/github/actions/workflow/status/mel0k1/SakuraTrap/ci.yml?branch=main&style=flat-square&label=CI)](https://github.com/mel0k1/SakuraTrap/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/mel0k1/SakuraTrap?style=flat-square)](LICENSE)
[![English](https://img.shields.io/badge/README-English-ff69b4?style=flat-square)](README.en.md)

</div>

SakuraTrap — форк [Fishstrap](https://github.com/fishstrap/fishstrap), который сам является форком [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap). Это лаунчер для Roblox на Windows 10 и новее: он устанавливает и обновляет игру, применяет FastFlags, моды и создаёт ярлыки вместо стандартного бутстраппера Roblox.

Главная идея SakuraTrap — не заставлять пользователя разбираться в сотнях FastFlags вручную. Вместо этого вы выбираете один профиль производительности, и лаунчер сам настраивает согласованный набор флагов: частота кадров, качество рендера, сглаживание и детализация геометрии меняются согласованно, а не поодиночке.

> [!WARNING]
> SakuraTrap не связан с Roblox Corporation. Roblox ограничивает локальные FastFlags allowlist'ом — флаги вне списка клиент игнорирует, поэтому часть настроек может не применяться. Это ограничение самой Roblox, а не лаунчера.

## Возможности

- **Профили FastFlags** — Default / Balanced / Performance / Quality одним выбором в настройках (см. таблицу ниже)
- Всё, что умеет Fishstrap: разблокировка частоты кадров, выбор рендера (Vulkan / D3D11), MSAA, детализация геометрии, моды (курсоры, звуки, старые текстуры), Discord Rich Presence, поддержка Roblox Studio, кастомные диалоги запуска
- Полный редактор FastFlags и редактор Global Basic Settings (лимит FPS, уровни качества, чувствительность мыши)
- Иконка сакуры и свежий ребрендинг

## Профили FastFlags

Находятся в **Настройки → FastFlags → Профиль производительности**. Профиль управляет только «своими» флагами: ваши ручные настройки и флаги из редактора не затрагиваются. Выбор профиля запоминается и восстанавливается при следующем запуске.

| Флаг | Назначение | Balanced | Performance | Quality | Default |
|---|---|---|---|---|---|
| `DFIntTaskSchedulerTargetFps` | Целевой FPS | 144 | 240 | 240 | — |
| `DFIntDebugFRMQualityLevelOverride` | Качество рендера (1–21) | 10 | 1 | 21 | — |
| `FIntDebugForceMSAASamples` | Сглаживание | — | 1 | 4 | — |
| `DFFlagDebugRenderForceTechnologyVoxel` | Упрощённое освещение | — | True | — | — |
| `DFIntCSGLevelOfDetailSwitchingDistance*` | Детализация мешей (0–3) | 1 | 0 | 3 | — |

`Default` снимает только флаги профиля, возвращая эти настройки под контроль Roblox и ваших ручных правок.

## Установка

1. Скачайте `SakuraTrap.exe` со страницы [Releases](https://github.com/mel0k1/SakuraTrap/releases)
2. Запустите — лаунчер сам установит Roblox или подхватит уже установленную версию
3. Откройте настройки, раздел FastFlags, выберите профиль производительности

## Сборка из исходников

Нужен Windows и .NET SDK 6.0 (версия зафиксирована в `global.json`). Репозиторий содержит сабмодуль `wpfui`, поэтому клонируйте с `--recursive`.

```sh
git clone --recursive https://github.com/mel0k1/SakuraTrap.git
cd SakuraTrap
dotnet publish Bloxstrap/Bloxstrap.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```

Готовый `SakuraTrap.exe` появится в `Bloxstrap/bin/Release/net6.0-windows/win-x64/publish/`. Сборка также прогоняется в CI на каждый push — артефакт можно забрать со страницы Actions.

## Планы

Дорожная карта идей — в [ROADMAP.md](ROADMAP.md). Баги и предложения — в [Issues](https://github.com/mel0k1/SakuraTrap/issues).

## Благодарности

- [Fishstrap](https://github.com/fishstrap/fishstrap) — прямая база форка (MIT)
- [pizzaboxer](https://github.com/pizzaboxer) и [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) — оригинальный проект (MIT)
- [WPF-UI](https://github.com/lepoco/wpfui) и все авторы библиотек, перечисленных в разделе «Лицензии» внутри приложения

## Лицензия

[MIT](LICENSE). Проект наследует код Fishstrap (MIT, © returnrqt) и Bloxstrap (MIT, © pizzaboxer) — их уведомления сохранены в [LICENSE](LICENSE) и [LICENSE.Bloxstrap](LICENSE.Bloxstrap).
