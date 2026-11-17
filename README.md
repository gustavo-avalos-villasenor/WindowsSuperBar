# WorkWindowsBar

WorkWindowsBar es una barra de tareas para Windows 10/11 pensada para trabajar con muchas ventanas abiertas. Reemplaza la barra de tareas nativa y trae de vuelta lo que Windows 11 quitó: **Quick Launch** fijo, **varias filas** de apps abiertas con su título visible, y anchos ajustables para cada zona.

Es un fork de [RetroBar](https://github.com/dremin/RetroBar) (Apache-2.0), que a su vez usa [ManagedShell](https://github.com/cairoshell/ManagedShell).

> *English summary: WorkWindowsBar is a work-focused Windows taskbar replacement (fork of RetroBar) with an always-visible Quick Launch, multi-row labelled task buttons, resizable Quick Launch / tray areas and a flat dark theme.*

## Qué tiene

- **Quick Launch siempre visible** a la izquierda, con iconos en filas. El ancho se ajusta arrastrando el separador.
  - Clic derecho en un espacio vacío → *Abrir carpeta de Inicio rápido*.
  - Para agregar un programa, arrástralo sobre la zona de iconos.
- **Apps abiertas en varias filas.** Cada botón es ancho y muestra el icono y el título. La barra muestra tantas filas como quepan en su altura, hasta 3 por defecto.
- **Bandeja de notificaciones redimensionable.** Los iconos se reparten de forma pareja en el espacio disponible.
- **Cierre rápido** (botón junto al reloj): el primer clic lo pone en rojo; el segundo, dentro de 3 s, manda cerrar **a la vez** todas las ventanas de la barra, como darle a la X a cada una. Las que tengan cambios sin guardar te preguntan y se quedan abiertas.
- **Tema "Moderno Oscuro"** (por defecto): plano, oscuro y con esquinas redondeadas. Los temas clásicos de RetroBar (Windows 95-98, XP, Vista…) siguen disponibles.
- Todo lo de RetroBar: bandeja nativa, miniaturas, barra en cualquier borde, varios monitores, ocultar automáticamente y temas o idiomas personalizados.

## Requisitos

- Windows 10 u 11 de 64 bits.
- La versión publicada es *self-contained*, así que no hace falta instalar .NET.
- Para compilar se necesita el [.NET 10 SDK](https://dotnet.microsoft.com/download).

## Instalar / usar

1. Descarga el artefacto `WorkWindowsBar-win-x64` desde *Actions*, o compílalo tú (ver abajo).
2. Copia la carpeta donde quieras dejarla, por ejemplo `C:\Tools\WorkWindowsBar`, y ejecuta `WorkWindowsBar.exe`.
3. Para que arranque con Windows: clic derecho en la barra → *Propiedades* → *Iniciar automáticamente al iniciar sesión*.
4. Para salir y volver a la barra de Windows: clic derecho en la barra → *Salir de WorkWindowsBar*.

**Ajustar tamaños:** desbloquea la barra (clic derecho → desmarcar *Bloquear la barra de tareas*) y arrastra:
- el borde superior de la barra, para cambiar la altura;
- el separador entre Quick Launch y las apps, para cambiar el ancho del Quick Launch;
- el separador a la izquierda de la bandeja, para cambiar el ancho de la bandeja.

## Configuración

Se guarda en `%LOCALAPPDATA%\WorkWindowsBar\settings.json`. Para editarla a mano, cierra la barra antes, porque al salir reescribe el archivo. Estos son los ajustes propios de WorkWindowsBar:

| Ajuste | Por defecto | Qué hace |
|---|---|---|
| `TaskRowHeight` | `32` | Alto objetivo (px) de cada fila de apps |
| `MaxTaskRows` | `3` | Máximo de filas de apps |
| `TaskLabelLines` | `1` | Líneas máximas del título de cada app |
| `TaskButtonWidthOverride` | `220` | Ancho preferido de cada botón (0 = el del tema) |
| `MultiLineTaskButtons` | `true` | Activa el diseño de filas y botones altos |
| `QuickLaunchWidth` | `0` | Ancho del Quick Launch (0 = automático) |
| `TrayIconsWidth` | `0` | Ancho de la bandeja (0 = automático) |
| `CustomStartIcon` | `""` | Icono del botón de Inicio: `""` = el del tema, `Cerdito` = el incluido, o la ruta a una imagen |
| `CustomStartIconSize` | `52` | Tamaño (px) del icono de Inicio personalizado |
| `QuickLaunchPath` | `%appdata%\Microsoft\Internet Explorer\Quick Launch` | Carpeta de accesos directos del Quick Launch |
| `RowCount` | `2` | Altura de la barra, en filas de RetroBar |

La primera vez que se ejecuta, si existe una configuración de RetroBar en `%LOCALAPPDATA%\RetroBar`, se copia automáticamente.

## Compilar

```powershell
dotnet publish WorkWindowsBar\WorkWindowsBar.csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:Platform=AnyCPU -o publish\WorkWindowsBar
```

El ejecutable queda en `publish\WorkWindowsBar\WorkWindowsBar.exe`. Antes de sobrescribir una carpeta en uso, cierra la barra que esté corriendo.

## Icono de Inicio

En *Propiedades* → *Icono de Inicio* puedes elegir entre **Del tema** (el original de cada tema; en "Moderno Oscuro", los cuatro cuadros) y **Cerdito**. Para agregar los tuyos, pon un `.png` (idealmente cuadrado y con fondo transparente) en `%LOCALAPPDATA%\WorkWindowsBar\StartIcons` y aparecerá en la lista con su nombre.

## Temas

Los temas son archivos XAML (`ResourceDictionary`) en `WorkWindowsBar\Themes`. Toma `Moderno Oscuro.xaml` como punto de partida para uno nuevo. También puedes poner temas propios en `%LOCALAPPDATA%\WorkWindowsBar\Themes` y elegirlos en *Propiedades*.

## Créditos y licencia

- Basado en **RetroBar**, © Sam Johnson y colaboradores: <https://github.com/dremin/RetroBar>
- Usa **ManagedShell** (Cairo Shell) y **gong-wpf-dragdrop**.
- Licencia: [Apache License 2.0](LICENSE). En [NOTICE](NOTICE) están el aviso de atribución y la lista de cambios respecto a RetroBar.

WorkWindowsBar es un fork independiente y no está afiliado a RetroBar ni respaldado por sus autores.
