function getError(e) {
	let err = e.responseText;
	if (err == null)
		err = e.statusText;
	if (err == null)
		err = 'Error ' + e.status;
	return err;
}