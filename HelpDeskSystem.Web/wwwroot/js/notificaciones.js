// wwwroot/js/notificaciones.js

let ultimoSonido = 0;

function reproducirSonido() {
    const ahora = Date.now();
    // Evitar ametralladora de sonidos (máx 1 cada segundo)
    if (ahora - ultimoSonido < 1000) return;

    try {
        var audio = new Audio('/sounds/notificacion_mensaje.mp3');
        var promise = audio.play();
        if (promise !== undefined) {
            promise.catch(error => console.log("Audio bloqueado por navegador"));
        }
        ultimoSonido = ahora;
    } catch (e) { console.error(e); }
}

function verificarYNotificar(mensaje, tipo, usuarioEstaEnElTicket) {
    // 1. EL SONIDO SIEMPRE SUENA (Si es mensaje de otro)
    reproducirSonido();

    // 2. LÓGICA VISUAL INTELIGENTE
    var ventanaTieneFoco = document.hasFocus();

    // CASO A: Estoy en el ticket Y estoy mirando la ventana -> NO MOLESTAR con Toast (ya veo el chat moverse)
    if (ventanaTieneFoco && usuarioEstaEnElTicket) {
        return;
    }

    // CASO B: Estoy en el ticket PERO tengo la ventana minimizada/fondo -> MOSTRAR TOAST
    // CASO C: Estoy en otra página (Dashboard) -> MOSTRAR TOAST
    mostrarNotificacion(mensaje, tipo);
}

function mostrarNotificacion(mensaje, tipo) {
    let contenedor = document.getElementById('toast-container');
    if (!contenedor) {
        contenedor = document.createElement('div');
        contenedor.id = 'toast-container';
        document.body.appendChild(contenedor);
    }

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${tipo} slide-in`;

    // Diseño limpio
    toast.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-chat-dots-fill fs-4 me-3"></i>
            <div>
                <span class="fw-bold d-block" style="font-size:0.9rem">${mensaje}</span>
                <small class="text-muted" style="font-size:0.75rem">Hace un momento</small>
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