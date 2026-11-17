# HANDOFF — WorkWindowsBar

Notas técnicas para quien continúe el proyecto.

## Qué es

WorkWindowsBar es un fork de **RetroBar v1.22.122** (C#/WPF, .NET 10 y .NET 6) que reemplaza la barra de tareas de Windows. Se basa en el enfoque de RetroBar porque ya resolvía lo difícil: AppBar, bandeja nativa, lista de ventanas, Quick Launch e inicio. El trabajo del fork se concentra en el diseño y la usabilidad para trabajar con muchas ventanas.

Upstream: <https://github.com/dremin/RetroBar>. Librería base: [ManagedShell](https://github.com/cairoshell/ManagedShell), vía NuGet.

## Estructura

```
WorkWindowsBar.sln
WorkWindowsBar/                 proyecto WPF (AssemblyName=WorkWindowsBar, RootNamespace=RetroBar)
  Taskbar.xaml(.cs)             ventana AppBar: Start, Quick Launch, lista de tareas, bandeja, reloj
  Controls/                     TaskList, TaskButton, Toolbar (Quick Launch), NotifyIconList, SpreadWrapPanel…
  Converters/                   IconListOrientation, ZeroToAutoConverter, PositiveToBoolConverter…
  Utilities/Settings.cs         todas las opciones (se serializan a settings.json)
  Themes/*.xaml                 temas (System.xaml = base; los demás lo sobrescriben)
  Languages/*.xaml              textos (English = respaldo; luego el idioma del sistema)
.github/workflows/build.yml     CI: publica win-x64 self-contained como artefacto
NOTICE                          atribución + lista de cambios (requerido por Apache-2.0)
```

**Decisión:** el namespace C# sigue siendo `RetroBar`. Solo se renombró lo visible (exe, textos, carpeta de datos, autostart y mutex). Así es fácil traer cambios de upstream con `git merge`/cherry-pick y la atribución queda clara. Si algún día se quiere renombrar el namespace, conviene hacerlo en un commit aislado.

## Cambios respecto a RetroBar

| Área | Archivos | Detalle |
|---|---|---|
| Renombre | `WorkWindowsBar.csproj`, `Program.cs` (mutex), `Extensions/ManagementExtensions.cs` (carpeta `%LOCALAPPDATA%\WorkWindowsBar` + migración desde `RetroBar`), `PropertiesWindow.xaml(.cs)` (valor Run `WorkWindowsBar`), `app.manifest`, `App.xaml.cs`, `Languages/*.xaml` | El enlace "Basado en RetroBar (GitHub)" en Propiedades apunta a upstream a propósito. |
| Filas de apps | `Controls/TaskList.xaml(.cs)` | En modo `MultiLineTaskButtons` (horizontal), `rows = clamp(floor(altoDisponible / TaskRowHeight), 1, MaxTaskRows)`. `ButtonHeight` (DP) se enlaza a `TaskButton.Height`. Ancho preferido = `TaskButtonWidthOverride`. |
| Título de app | `Controls/TaskButton.xaml(.cs)` | `ApplyLabelLayout()`: wrap + trimming, `LineHeight` fijo y `MaxHeight` = líneas que caben (tope `TaskLabelLines`). Se recalcula con `SizeChanged`. |
| Quick Launch | `Taskbar.xaml` (Width ← `QuickLaunchWidth`), `Controls/TaskList.xaml.cs` (`Thumb_DragDelta`: el grip de la lista de tareas cambia el ancho del Quick Launch), `Converters/IconListOrientation.cs` (filas si hay ancho fijo), `Controls/Toolbar.xaml(.cs)` (menú contextual "Abrir carpeta de Inicio rápido") | Carpeta por defecto: `%appdata%\Microsoft\Internet Explorer\Quick Launch`. En Win11 la de upstream (`User Pinned\TaskBar`) no existe. |
| Bandeja | `Taskbar.xaml(.cs)` (`TrayResizeThumb`), `Controls/NotifyIconList.xaml(.cs)`, `Controls/SpreadWrapPanel.cs` | Con `TrayIconsWidth > 0` los iconos se reparten por toda el área. Si no caben, se usa el wrap normal. |
| Tema | `Themes/Moderno Oscuro.xaml` | Plano y oscuro, botón de inicio solo con icono y sin indicador bajo las apps. Es el tema por defecto en `Settings.cs`. |
| Notificaciones | `Controls/TaskButton.xaml.cs` (`UpdateAttentionState`) | Un botón que pidió atención (FlashWindowEx) queda fijo en `TaskButtonFlashing` hasta que la ventana pasa a Active. ManagedShell solo alterna Flashing/Inactive con cada parpadeo y terminaba apagado. |
| Icono de Inicio | `Utilities/DictionaryManager.cs` (`ApplyCustomStartIcon`, `GetStartIcons`, `ResolveStartIcon`), `Resources/StartIcons/*.png`, `PropertiesWindow.xaml(.cs)` (`cboStartIconSelect`), `Settings.CustomStartIcon/CustomStartIconSize` | Pone `StartIconImage` y un estilo `StartIcon` (tamaño, escalado HighQuality) directo en `Application.Resources`, que gana sobre el tema. `""` = icono del tema. Iconos de usuario en `%LOCALAPPDATA%\WorkWindowsBar\StartIcons`. |
| Cierre rápido | `Taskbar.xaml` (`QuickCloseButton`), `Taskbar.xaml.cs` (`QuickCloseButton_OnClick`, arm/disarm), `Controls/TaskList.xaml.cs` (`GetVisibleWindows`) | Dos clics (el primero "arma" el botón 3 s, en rojo vía `Tag="armed"`). Envía `WM_SYSCOMMAND/SC_CLOSE` con `PostMessage` a todas las ventanas visibles de la barra a la vez. No usa `ApplicationWindow.Close()` de ManagedShell porque es secuencial (trae cada ventana al frente + `SendMessageTimeout` de 200 ms). |
| Updates | `Utilities/Updater.cs` | Desactivado (`UpdateFeedAvailable = false`), porque el feed de upstream ofrecería instalar RetroBar encima. |
| Build | `Directory.Build.props` | Se quitó Nerdbank.GitVersioning (requería repo git al compilar). La versión está fija en `<Version>`. |
| Eliminado | `installer.iss`, workflows de upstream, `version.json`, `retrobar-preview.png` | Eran específicos de los releases de RetroBar. |

## Compilar y probar

```powershell
dotnet build   WorkWindowsBar\WorkWindowsBar.csproj -c Release -f net10.0-windows -p:Platform=AnyCPU
dotnet publish WorkWindowsBar\WorkWindowsBar.csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:Platform=AnyCPU -o publish\WorkWindowsBar
```

- **Cerrar antes de publicar.** Para publicar sobre una carpeta en uso, cierra antes la barra desde el menú → Salir. Si hay que forzarlo (`Stop-Process -Name WorkWindowsBar`), vuelve a lanzarla enseguida: un cierre forzado puede dejar oculta la barra nativa hasta reiniciar el Explorador.
- **Cambios en temas.** Los `.xaml` de `Themes` se copian sueltos al output y se cargan en runtime. Para probar cambios de tema basta con copiar el archivo a la carpeta del exe y reiniciar la barra; no hace falta recompilar.
- **settings.json.** Lo reescribe la app en cada cambio, así que hay que editarlo con la barra cerrada.
- **No hay tests automatizados.** La verificación ha sido manual, con capturas de la barra en Windows 11 (build 26300) y 2 monitores.

## Pendientes e ideas

- Volver a activar el updater cuando el fork tenga su propio feed de versiones (por ejemplo, GitHub Releases).
- Instalador propio (Inno Setup o MSIX) en lugar del portable.
- Doble clic en los grips para volver a "automático" (0) sin editar `settings.json`.
- Exponer en *Propiedades* los ajustes nuevos (`TaskRowHeight`, `MaxTaskRows`, `TaskLabelLines`, anchos), que hoy solo están en el JSON.
- `TrayIconsWidth`/`QuickLaunchWidth` solo están pensados para la barra horizontal (abajo o arriba). En los bordes izquierdo y derecho no se probaron.
- Icono propio (hoy usa `Resources/retrobar.ico` de upstream).
- Opcional: un color de acento configurable en "Moderno Oscuro" (hoy es `#4CC2FF` fijo).

## Traer cambios de upstream

```bash
git remote add upstream https://github.com/dremin/RetroBar.git
git fetch upstream
git merge upstream/master   # conflictos esperables: archivos de la tabla de arriba
```

Upstream tiene la carpeta del proyecto en `RetroBar/`. Aquí se llama `WorkWindowsBar/`, así que git detecta el renombre si los archivos no cambiaron demasiado. Si no lo detecta, conviene aplicar los commits con `cherry-pick` y ajustar las rutas.
