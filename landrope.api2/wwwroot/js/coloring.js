const $No_Information = 0;
const $Belum_Bebas__SHM = 1;
const $Belum_Bebas__Hibah__Belum_PBT = 2;
const $Belum_Bebas__Hibah__Sudah_PBT = 3;
const $Kampung = 4;
const $Bengkok_Desa = 5;
const $Bebas = 6;

var $statuscolor = ['#000000', '#ff0000', '#00ff00', '#ffff00', '#800080', '#800080', '#0000ff'];

var statusattr = [
	{
		color: '#000000',
		fillColor: $statuscolor[0],
		alpha: 1,
		fillAlpha: 0,
		weight:1,
		interactive : true
	},
	{
		color: '#000000',
		fillColor: $statuscolor[1],
		alpha: 1,
		fillAlpha: 1,
		weight: 1,
		interactive : true
	},
	{
		color: '#000000',
		fillColor: $statuscolor[2],
		alpha: 1,
		fillAlpha: 1,
		weight: 1,
		interactive : true
	},
	{
		color: '#000000',
		fillColor: $statuscolor[3],
		alpha: 1,
		fillAlpha: 1,
		weight: 1,
		interactive : true
	},
	{
		color: '#000000',
		fillColor: $statuscolor[4],
		alpha: 1,
		fillAlpha: 1,
		weight: 1,
		interactive : true
	},
	{
		color: '#000000',
		fillColor: $statuscolor[5],
		alpha: 1,
		fillAlpha: 1,
		weight: 1,
		interactive : true
	},
	{
		color: '#000000',
		fillColor: $statuscolor[6],
		alpha: 1,
		fillAlpha: 1
		,
		weight: 1,
		interactive : true
	}
];

const SKSkin = {
	color: '#000080',
	fillColor: '#fff',
	alpha: 1,
	fillAlpha: 1,
	weight: 1,
	interactive : true
};

const SKOver = {
	color: '#000080',
	fillColor: '#00000000',
	alpha: 1,
	fillAlpha: 0,
	weight: 4,
	interactive : false
};

function Style(feature) {
	var attrib;
	statuscd = feature.properties["status"];
	if (typeof statuscd == 'undefined') {
		attrib = {
			color: '#fff',
			fillColor: '#800080',
			alpha: 1,
			fillAlpha: 1,
			weight:1,
			interactive : false
		};
		return attrib;
	}
	istatus = parseInt(statuscd);
	try {
		attrib = statusattr[istatus];
	}
	catch (e) {
		attrib = statusattr[0];
	}

	return attrib;
}
