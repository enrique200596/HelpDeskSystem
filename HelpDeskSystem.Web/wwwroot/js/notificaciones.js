// wwwroot/js/notificaciones.js
let ultimoSonido = 0;

function reproducirSonido() {
    const ahora = Date.now();
    if (ahora - ultimoSonido < 1000) return; // Evitar spam muy rápido

    try {
        // RUTA CORREGIDA: notificacion_mensaje.mp3
        var audio = new Audio('/sounds/notificacion_mensaje.mp3');
        var promise = audio.play();
        if (promise !== undefined) {
            promise.catch(error => console.log("Audio bloqueado (falta interacción)"));
        }
        ultimoSonido = ahora;
    } catch (e) { console.error(e); }
}

function verificarYNotificar(mensaje, tipo, usuarioEstaEnElTicket) {
    // 1. REGLA: El sonido SIEMPRE debe escucharse (si es mensaje nuevo)
    reproducirSonido();

    // 2. REGLA VISUAL:
    // Si la ventana tiene foco Y el usuario está viendo el ticket correcto...
    // NO MOSTRAR EL TOAST (Visual).
    var tieneFoco = document.hasFocus();
    if (tieneFoco && usuarioEstaEnElTicket) {
        return;
    }

    // Si no está mirando, mostrar Toast
    mostrarNotificacion(mensaje, tipo);
}

function mostrarNotificacion(mensaje, tipo) {
    // (Nota: Quitamos reproducirSonido() de aquí para no duplicar, ya lo llamamos arriba)

    let contenedor = document.getElementById('toast-container');
    if (!contenedor) {
        contenedor = document.createElement('div');
        contenedor.id = 'toast-container';
        document.body.appendChild(contenedor);
    }

    const toast = document.createElement('div');
    // Usamos 'toast-info' (azul) o 'toast-danger' (rojo) según corresponda
    toast.className = `toast-notification toast-${tipo} slide-in`;

    // HTML mejorado para mostrar remitente y título
    toast.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-chat-dots-fill fs-4 me-3"></i>
            <div>
                <span class="fw-bold d-block" style="font-size:0.9rem">${mensaje}</span>
                <small class="text-muted" style="font-size:0.75rem">Hace un instante</small>
            </div>
        </div>
    `;

    contenedor.appendChild(toast);
    setTimeout(() => { toast.classList.add('show'); }, 100);
    setTimeout(() => {
        toast.classList.remove('show');
        toast.addEventListener('transitionend', () => toast.remove());
    }, 5000);
}