
const url = window.AppConfig?.realtimeHubUrl || "https://localhost:62350/hubs/realtime";

async function getConnectionToken() {
	try {
		const res = await fetch('/api/realtime/token', { method: 'POST' });
		if (!res.ok) return null;
		const data = await res.json();
		return data.token;
	} catch {
		return null;
	}
}

const connection = new signalR.HubConnectionBuilder()
	.withUrl(url, {
		accessTokenFactory: () => getConnectionToken(),
		transport: signalR.HttpTransportType.WebSockets,
		withCredentials: true
	})
	.withAutomaticReconnect()
	.build();

// simple audio beeper (inline)
function playNotificationSound() {
	try {
		const ctx = new (window.AudioContext || window.webkitAudioContext)();
		const o = ctx.createOscillator();
		const g = ctx.createGain();
		o.type = "sine";
		o.frequency.value = 880; // A5
		o.connect(g);
		g.connect(ctx.destination);
		g.gain.setValueAtTime(0.001, ctx.currentTime);
		g.gain.exponentialRampToValueAtTime(0.2, ctx.currentTime + 0.01);
		g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);
		o.start();
		o.stop(ctx.currentTime + 0.26);
	} catch (e) {
		// ignore if autoplay blocked
	}
}

// دریافت پیام
connection.on("ReceiveUnreadNotification", message => {

	if (Array.isArray(message)) {

		message.forEach(z => {

			var path = z?.viewPath ?? "#";
			$("#notificationSection")
				.append(

					`
					<div class="d-flex flex-stack py-4 px-3 notification-item"
					data-row="notification">

				    <div class="d-flex align-items-center">

				 
					   <div class="symbol symbol-40px me-4">
						  <div class=" d-flex flex-column justify-content-center gap-1" >

							 <div class="symbol-label bg-light-danger" style="    width: 20px;    height: 20px;">
								  <span class="notification-icon"
									  data-action="readNotification"
									  data-id="${z.id}"
									   >
									<i class="ki-duotone ki-eye fs-3 text-danger">
									    <span class="path1"></span>
									    <span class="path2"></span>
									    <span class="path3"></span>
									</i>
								 </span>
							 </div>
							

						  <div class="symbol-label bg-light-primary" style="    width: 20px;    height: 20px;">
							 <a href="${path}"
							    class="notification-link-icon"
							     >
								<i class="ki-duotone ki-arrow-right fs-4 text-gray-600">
								    <span class="path1"></span>
								    <span class="path2"></span>
								</i>
							 </a>
							  </div>
						  </div>
					   </div>

					 
					   <div class="me-2">
						  <a href="${path}"
							class="fs-6 text-gray-800 text-hover-primary fw-bold d-block">
							${z.title}
						  </a>
						  <span class="text-muted fs-8">
							 ${z.body}
						  </span>
					   </div>

				    </div>

				  
				    <span class="badge badge-light fs-8 text-gray-600" style="direction:ltr;">
					${z.createdOnShamsiDateTime}
				    </span>
				</div>
					`

				)
		})
	}


	// play short sound
	playNotificationSound();

	CalculationCountUnreadNotifications();


});

// دریافت پیام
connection.on("ReceiveNotification", message => {

	$("#notificationSection")
		.append(
			`	<div class="d-flex flex-stack py-4">
				 
						<div class="d-flex align-items-center">
				 
							<div class="symbol symbol-35px me-4">
								<span class="symbol-label bg-light-primary" data-action='readNotification' data-id='${message.id}'>
									 
									<i class="ki-duotone ki-eye fs-2 text-danger">
										 <span class="path1"></span>
										 <span class="path2"></span>
										 <span class="path3"></span>
									</i>
								</span>
							</div>
						 
							<div class="mb-0 me-2">
								<a href="#" class="fs-6 text-gray-800 text-hover-primary fw-bold">${message.title}</a>
							 
							</div>
				 
						</div>
					 
						<span class="badge badge-light fs-8">${message.createdOnShamsiDateTime}</span>
					 
					</div>`
		)


	// play short sound
	playNotificationSound();

	CalculationCountUnreadNotifications();

});


connection.on("EntityChanged", event => {
	console.log("Entity changed:", event);


	if (event.entityName === "User" && event.operation === "Create") {

		toastr.info(event.message || `موجودیت ${event.entityName} با شناسه ${event.entityId} ایجاد شد`, "تغییر موجودیت");
	}

 
});

// اتصال
connection.start()
	.then()
	.catch(err => console.error(err));

// clear highlight when user opens the menu (click on the bell)
document.addEventListener("click", function (ev) {
	const bellBtn = document.getElementById("kt_menu_item_notification");
	if (!bellBtn) return;
	if (bellBtn.contains(ev.target)) {
		// toggle off on click; next notification will add it again
		bellBtn.classList.remove("has-notification");
	}
});


window.UpdateCurrentPage = async function (path,title) {

	if (connection && connection.state === signalR.HubConnectionState.Connected) {
		connection.invoke("UpdateOpenedPage", path, title)
			.catch(err => console.error("UpdateOpenedPage error:", err));
	}
}



 

window.ClosePage = async function (path) {

	connection.invoke("ClosePage", path).catch(() => { });
}
