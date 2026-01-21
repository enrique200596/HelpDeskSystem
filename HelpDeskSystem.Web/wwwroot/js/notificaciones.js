// wwwroot/js/notificaciones.js

let ultimoSonido = 0;
let audioHabilitado = false;

// CORRECCIÓN: Intentar desbloquear el audio en el primer clic del usuario
document.addEventListener('click', () => {
    if (!audioHabilitado) {
        audioHabilitado = true;
        console.log("Audio de notificaciones habilitado por interacción del usuario.");
    }
}, { once: true });

function reproducirSonido() {
    const ahora = Date.now();
    // Evitar saturación de sonidos (máx 1 cada segundo)
    if (ahora - ultimoSonido < 1000) return;

    // Solo intentar si el usuario ya interactuó con la web
    if (!audioHabilitado) return;

    try {
        var audio = new Audio('/sounds/notificacion_mensaje.mp3');
        var promise = audio.play();

        if (promise !== undefined) {
            promise.catch(error => {
                // El navegador aún puede bloquearlo si no detecta interacción real
                console.warn("Audio bloqueado: Requiere interacción previa con la página.");
            });
        }
        ultimoSonido = ahora;
    } catch (e) {
        console.error("Error al reproducir sonido:", e);
    }
}

function verificarYNotificar(mensaje, tipo, usuarioEstaEnElTicket) {
    // 1. EL SONIDO (Solo si es actividad externa)
    reproducirSonido();

    // 2. LÓGICA VISUAL
    var ventanaTieneFoco = document.hasFocus();

    // Si estoy viendo el chat activamente, no mostramos el Toast (el mensaje ya aparece en burbuja)
    if (ventanaTieneFoco && usuarioEstaEnElTicket) {
        return;
    }

    mostrarNotificacion(mensaje, tipo);
}

function mostrarNotificacion(mensaje, tipo) {
    let contenedor = document.getElementById('toast-container');
    if (!contenedor) {
        contenedor = document.createElement('div');
        contenedor.id = 'toast-container';
        contenedor.style.cssText = "position: fixed; top: 20px; right: 20px; z-index: 9999;";
        document.body.appendChild(contenedor);
    }

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${tipo} shadow-lg`;

    // CORRECCIÓN: Estilos base inyectados para asegurar visibilidad si el CSS falla
    toast.style.cssText = "background: white; border-left: 4px solid #C8102E; padding: 15px; margin-bottom: 10px; border-radius: 8px; min-width: 250px; transition: all 0.3s ease; opacity: 0; transform: translateX(20px);";

    toast.innerHTML = `
        <div style="display: flex; align-items: center;">
            <div style="color: #C8102E; margin-right: 12px; font-size: 1.5rem;">
                <i class="bi bi-chat-dots-fill"></i>
            </div>
            <div>
                <span style="display: block; font-weight: bold; color: #333; font-size: 0.9rem;">${mensaje}</span>
                <small style="color: #666; font-size: 0.75rem;">Nueva actividad</small>
            </div>
        </div>
    `;

    contenedor.appendChild(toast);

    // Animación de entrada
    setTimeout(() => {
        toast.style.opacity = "1";
        toast.style.transform = "translateX(0)";
    }, 100);

    // Auto-eliminar
    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transform = "translateX(20px)";
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

// UTILIDADES PARA EL CHAT (Invocadas desde DetalleTicket.razor)
window.utilidadesChat = {
    bajarScroll: function (id) {
        const elemento = document.getElementById(id);
        if (elemento) {
            elemento.scrollTo({
                top: elemento.scrollHeight,
                behavior: 'smooth'
            });
        }
    },
    ponerFoco: function (id) {
        const elemento = document.getElementById(id);
        if (elemento) elemento.focus();
    }
};