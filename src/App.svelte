<script>
	import { slide} from 'svelte/transition'

	let result='';
	let failed = false;
	let showbuttons = true;
	let website = '';
	$:showresult = result;


	async function saveOkOrNot(isok) {
	try {
		var res = await fetch ("/sendok.ashx", { method: "POST", body: new URLSearchParams({ isok: isok, website: website }) });
		result = await res.text();
		failed = !res.ok;
	} catch (e) {
		result = "Versturen mislukt, controleer de verbinding en probeer het opnieuw.";
		failed = true;
	}
	getlastinfo();
	}

	function hidebuttons(){
	showbuttons = false;
	setTimeout(()=>{showbuttons=true;}, 5000);
	}
	async function ok() {
	hidebuttons();
	await saveOkOrNot(true);
	}
	async function notok() {
	hidebuttons();
	await saveOkOrNot(false);
	}

	let laatsteokmeldingen;
	let nietokmeldingen;
	async function getlastinfo(){
	var res = await fetch ("/getlastok.ashx" );
	laatsteokmeldingen = await res.text();

	var res = await fetch ("/getlastnotok.ashx");
	nietokmeldingen = await res.text();
	}

	getlastinfo();
</script>

<style>
  button {
    width: 200px;
    height: 200px;
    margin: 40px;
    font-weight: bold;
    font-size: 18px;
    color: white;
  }

  .ok {
    background-color: green;
  }

  .notok {
    background-color: red;
  }

  .result{
	  font-size:16px;
	  border:1px solid grey;

  }

  .error {
	  color: red;
	  font-weight: bold;
  }

  .hp {
	  position: absolute;
	  left: -10000px;
	  width: 1px;
	  height: 1px;
	  overflow: hidden;
  }
</style>

<main> 
	<div class="hp" aria-hidden="true">
		<label>Laat dit veld leeg <input type="text" name="website" tabindex="-1" autocomplete="off" bind:value={website}></label>
	</div>
	{#if showbuttons}
	<div transition:slide>
  <button on:click={ok} class="ok">OK</button>
  <button on:click={notok} class="notok">NOT OK</button>
	</div>
	{#if failed}
  <div class="result error">{showresult}</div>
	{/if}
	{:else}
  <div class="result" class:error={failed}>{showresult}</div>
	{/if}
<div style="background-color: blueviolet;">
	Laatste OK meldingen:<br/>
	{@html laatsteokmeldingen}
<br/>
<br/>
	Laatste NO OK meldingen:<br/>
	{@html  nietokmeldingen}
</div>
</main>
 