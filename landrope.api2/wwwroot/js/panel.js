for (var i = 1; i < 7; i++) {
	$("#c_" + i).css("background-color", $statuscolor[i]);
}
function SetCombo(combo, items) {
	const listBox = document.querySelector(combo);
	//if (listBox.items.length>0)
	try {
		listBox.clearItems();
	}
	catch (ex) {}
	//combo.empty();
	var itms = [];
	items.forEach(i => {
		const elem = document.createElement("smart-list-item");
		listBox.appendChild(elem);
		elem.outerHTML = '<smart-list-item value="' + i.key + '">' + i.desc + '</smart-list-item>';
	});
}

var getKey = (sel) => sel.children("option:selected").id;

function w3_open() {
	$("#sidebar").addClass('w3-animate-right');
	$("#sidebar").removeClass('w3-animate-right-out');
	$("#sidebar").css({ display: "block" });// .show("fast");// 
}

function w3_close() {
	$("#sidebar").removeClass('w3-animate-right');
	$("#sidebar").addClass('w3-animate-right-out');
	window.setTimeout(() => { $("#sidebar").css({ display: "none" }) }, 700);
	//$("#sidebar").css({ display: "none" });//.hide('slow');
}

function modeChanged() {
	let checked = $('#showmode').jqxSwitchButton('checked');
	if (checked) {
		$("#leg_detail").css("display", "none");
		$("#leg_sk").css("display", "block");
	}
	else {
		$("#leg_sk").css("display", "none");
		$("#leg_detail").css("display", "block");
	}
	selProChanged();
}

$(document).ready(() => {
	$('#showmode').jqxSwitchButton({ height: 19, width: 41, checked: false });
	$('#showmode').on('change', (event) => { modeChanged() }); 
	$('#login').jqxWindow({
		isModal: true,
		//okButton: $("#submit"),
		position: { x: (innerWidth - 300) / 2, y: (innerHeight - 200)/2 },
		showCloseButton: false,
		showCollapseButton: false,
		width: 300,
		//autoOpen:true,
		initContent: function () {
			$('#login').jqxWindow('focus');
		}
	});
	$('#login').jqxWindow('open');

	//$("#c_1").jqxToggleButton({ width: '18', height:'100%', toggled: true, template:'success' });
	initialStatesView();
	$("#selpro").change(function () {
		window.setTimeout(selProChanged, 100);
	});
	//$("#selvil").change(function () {
	//	window.setTimeout(selVilChanged, 100);
	//});

	//selProChanged();
});

function listProject() {
	$.ajax({
		url: $myroot + '/api/entities/project/list?token=' + $const.token, method: 'POST', data: {}, dataType: "json", xhrFields: {
			withCredentials: true
		} })
		.done(data => {
			var items = [];
			data.forEach(d => {
				let item = { key: d.key, desc: d.identity };
				items.push(item);
			});
			try {
				$("#selpro").jqxListBox({ width: '100%', source: items, multipleextended: true, height: 200, displayMember: 'desc', valueMember: 'kwy' });
			}
			catch (x) {
				alert(x);
			}
			//SetCombo("#selpro", items);
			//selProChanged();
		})
		.catch(function (e) {
			alert(getError(e));
		})
}
function selProChanged() {
	let checked = $('#showmode').jqxSwitchButton('checked');
	let list = $("#selpro");
	let items = list.jqxListBox('getSelectedItems');
	//const listBox = document.querySelector("#selpro");
	let keys = [];
	items.forEach(i => keys.push(i.originalItem.key));
	//var data = { t: checked ? 't' : 's', key: keys.toString() };
	ShowMap(keys);

	//const switchButton = document.querySelector('#showmode');
	//let checked = switchButton.checked;
	//let items = listBox.selectedValues;
	//let combo = items.toString();
	//if (combo == '') {
	//	clearMap();
	//}
	//var data = { t: checked? 't' : 's', key: combo };
	//ShowMap(data);
	//$("#namadesa").css('visibility', 'hidden');
	//$("#btn1").css('visibility', 'hidden');
	//$.ajax({ url: $myroot + 'api/entities/village/list', method: 'POST', data: { projkey: prokey }, dataType: "json" })
	//	.done(data => {
	//		var items = [{ key: "*" + prokey, desc: "Semua (kulit SK)", selected: true }];
	//		//var i = 0;
	//		data.forEach(d => {
	//			let item = { key: d.key, desc: d.identity, selected: false };
	//			items.push(item);
	//		});
	//		SetCombo($("#selvil"), items);
	//		selVilChanged();
	//	})
	//	.catch(err => { alert(err); })
}
function selVilChanged() {
	var vilkey = getKey($("#selvil"));
	var data = { t: 'g', key: vilkey }
	if (vilkey.substring(0, 1) == "*") {
		data = { t: 's', key: vilkey.substr(1) };
	}
	ShowMap(data);
}

function detailClick(vilkey, vilname) {
	var data = { t: 'g', key: vilkey }
	ShowMap(data);
	$("#namadesa").css('visibility', 'visible');
	$("#namadesa").html("Desa <B>" + vilname + "</b>");
	$("#btn1").css('visibility', 'visible');
}

const states = [false, true, true, false, false, false, false, false, true];
const defclasses = ['', 'belum-bebas-shm', 'hibah-belum-pbt', 'hibah-sudah-pbt', 'kampung', 'bengkok-desa','bebas'];
const inactive = 'inactive';

function c_clicked(event) {
	if (event.srcElement = null || event.srcElement.id === undefined || event.srcElement.id.substr(0, 2) != 'c_')
		return;
	let id = event.srcElement.id.substr(2);
	var nid;
	try {
		nid = parseInt(id);
		states[nid] = !states[nid];
	}
	catch (e) {
		return;
	}

	if (nid == 4)
		states[5] = states[4];
	if (nid == 6)
		states[7] = states[6];
	if (nid == 2)
		states[8] = states[2];

	if (states[nid]) {
		$(event.srcElement).removeClass(inactive);
		$(event.srcElement).addClass(defclasses[nid]);
	}
	else {
		$(event.srcElement).addClass(inactive);
		$(event.srcElement).removeClass(defclasses[nid]);
	}
	selProChanged();
}

function initialStatesView(){
	for (var i = 1; i < 7; i++) {
		if (i == 5)
			continue;
		let element = $("#c_" + i);
		if (states[i]) {
			element.removeClass(inactive);
			element.addClass(defclasses[i]);
		}
		else {
			element.addClass(inactive);
			element.removeClass(defclasses[i]);
		}
	}
}

function doLogin() {
	var username = $("#username").val();
	var password = $("#password").val();

	var res = $.ajax({
		url: $myroot + '/home/login',  
		data: { username: username, password: password },
		type: 'POST', async: false, dataType: 'json'
	});
	if (res.status != 200) {
		window.alert(res.responseText);
		return;
	}
	$const.token = res.responseText;
	$("#login").jqxWindow('close');
	listProject();
	initMap();
}